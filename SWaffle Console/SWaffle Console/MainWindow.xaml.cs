using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SWaffle_Console
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private AssetBrowser? browserWindow;
        private bool isRefreshing = false;
        private bool receivingItems = false;
        private Dictionary<string, ushort> pendingItemLookup = new();

        private TcpClient client;
        private StreamWriter writer;
        private StreamReader reader;

        private List<string> commandList = new List<string> { "hello", "give", "finditem" };

        private Dictionary<string, ushort> itemLookup = new();
        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 7777);

                var stream = client.GetStream();
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                reader = new StreamReader(stream, Encoding.UTF8);
                Log("Connected to Remote Console");

                _ = Task.Run(ReadLoop);
            }
            catch (Exception ex)
            {
                Log("Failed to connect: " + ex.Message);
            }

            if(writer == null)
            {
                Log("Writer Not Ready Yet - Console not connected?");
                return;
            }

            if(client == null || !client.Connected)
            {
                Log("Client Not onnected - Cannot Send Command.");
                return;
            }
        }

        private async Task ReadLoop()
        {
            try
            {
                string line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if(line == "[ItemsListStart]")
                    {
                        receivingItems = true;
                        pendingItemLookup.Clear();
                        continue;
                    }

                    if(line == "[ItemsListEnd]")
                    {
                        receivingItems = false;

                        Dispatcher.Invoke(() =>
                        {
                            itemLookup = new Dictionary<string, ushort>(pendingItemLookup);
                            browserWindow?.RefreshItems(itemLookup);
                            isRefreshing = false;
                            Log("[AssetBrowser] Items List Refresed");
                        });

                        continue; 
                    }

                    if(receivingItems && line.StartsWith("[Item] "))
                    {
                        var data = line.Substring(7).Split('|');
                        if(data.Length == 2 &&
                            ushort.TryParse(data[0], out ushort id) &&
                            !string.IsNullOrWhiteSpace(data[1]))
                        {
                            pendingItemLookup[data[1]] = id;
                        }
                        continue;
                    }
                    Dispatcher.Invoke(() => Log(line));
                    
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log(" [Error] Disconnected: " + ex.Message));
            }

        }

        private void HandleIncomingLine(string line)
        {
            if(line.StartsWith("ITEMLIST:", StringComparison.Ordinal))
            {
                var payload = line.Substring("ITEMLIST:".Length);
                var parts = payload.Split('|');
                if(parts.Length == 2 && ushort.TryParse(parts[1], out ushort id))
                {
                    itemLookup[parts[0]] = id;

                    if (!commandList.Contains(parts[0]))
                        commandList.Add(parts[0]);
                }
                return;
            }

            Log(line);
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string input = CommandInput.Text.Trim();
            if (string.IsNullOrEmpty(input) || writer == null)
                return;

                writer.WriteLine(input);
                CommandInput.Clear();
                Log("> " + input);
            }

        private void Log(string message)
        {
            SolidColorBrush color = Brushes.Gray;

            if (message.Contains("[Error]") || message.Contains("[Exception]"))
                color = Brushes.Red;
            else if (message.Contains("[Warning]"))
                color = Brushes.Orange;
            else if (message.Contains("[Info]"))
                color = Brushes.LightGreen;

            var run = new Run(message) { Foreground = color };
            var paragrah = new Paragraph(run);

            LogBox.Document.Blocks.Add(paragrah);
            LogBox.ScrollToEnd();
        }

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Send_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                string curret = CommandInput.Text;
                var match = commandList.FirstOrDefault(c => c.StartsWith(curret));
                if (!string.IsNullOrEmpty(match))
                {
                    CommandInput.Text = match;
                    CommandInput.CaretIndex = match.Length;
                }
                e.Handled = true;
            }
        }

        private async void OpenAssetBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (writer == null || !client.Connected)
            {
                Log("Cannot open Asset Browser — not connected.");
                return;
            }


            if (browserWindow == null || !browserWindow.IsVisible)
            {
                browserWindow = new AssetBrowser(itemLookup, SendCommandFromBrowser);
                browserWindow.Owner = this;
                browserWindow.Show();

                isRefreshing = true;

                await Task.Delay(200);
                writer.WriteLine("listitems");
            }
            else
            {
                browserWindow.Focus();
                browserWindow.RefreshItems(itemLookup);

                if (!isRefreshing)
                {
                    isRefreshing = true;
                    await Task.Delay(200);
                    writer.WriteLine("listitems");
                }
            }
        }


        private void SendCommandFromBrowser(string cmd)
        {
            if (writer == null) return;
            writer.WriteLine(cmd);
            Log("> " + cmd);
        }
    }
}