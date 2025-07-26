using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SWaffle_Console
{
    /// <summary>
    /// Interaction logic for AssetBrowser.xaml
    /// </summary>
    public partial class AssetBrowser : Window
    {
        private Dictionary<string, ushort> itemLookup = new();
        private Action<string> sendCommand;
        private HashSet<string> favorites = new();
        private const string favoritesFile = "favorites.txt";

        public AssetBrowser(Dictionary<string, ushort> items, Action<string> sendCommandCallback)
        {
            InitializeComponent();
            itemLookup = items;
            sendCommand = sendCommandCallback;
            PopulateTree();
            LoadFavorites();
        }

        public void RefreshItems(Dictionary<string, ushort> newItems)
        {
            itemLookup = newItems;
            AssetTree.Items.Clear();

            var grouped = itemLookup.OrderBy(kvp => kvp.Key).GroupBy(kvp => kvp.Key[0]);

            foreach (var group in grouped)
            {
                var groupItem = new TreeViewItem { Header = group.Key.ToString(), Style = (Style)FindResource("GroupItemStyle")};
                foreach (var item in group)
                {
                    groupItem.Items.Add(new TreeViewItem
                    {
                        Header = item.Key,
                        Tag = item.Value,
                        Style = (Style)FindResource("AssetItemStyle")
                    });
                }

                AssetTree.Items.Add(groupItem);
            }
        }
        public void PopulateTree()
        {
            var grouped = itemLookup.GroupBy(x => x.Key[0]);

            foreach (var group in grouped)
            {
                var groupItem = new TreeViewItem { Header = group.Key.ToString() };
                foreach (var item in group)
                {
                    groupItem.Items.Add(new TreeViewItem { Header = item.Key, HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Top });
                }
                AssetTree.Items.Add(groupItem);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();
            var filtered = itemLookup
                .Where(x => x.Key.ToLower().Contains(query))
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
            UpdateTree(filtered);
        }

        private void UpdateTree(Dictionary<string, ushort> items)
        {
            AssetTree.Items.Clear();

            var grouped = items.OrderBy(k => k.Key).GroupBy(k => k.Key[0]);

            foreach (var group in grouped)
            {
                var groupItem = new TreeViewItem { Header = group.Key.ToString() };

                foreach (var item in group)
                {
                    string label = favorites.Contains(item.Key) ? "★ " + item.Key : item.Key;

                    groupItem.Items.Add(new TreeViewItem
                    {
                        Header = label,
                        Tag = item.Value,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Center
                    });
                }

                AssetTree.Items.Add(groupItem);
            }
        }


        private void GiveItem_Click(object sender, RoutedEventArgs e)
        {
            if (AssetTree.SelectedItem is TreeViewItem selected &&
                selected.Parent is TreeViewItem)
            {
                string itemName = selected.Header.ToString().Replace("★ ", "");
                if (itemLookup.TryGetValue(itemName, out ushort id))
                {
                    sendCommand?.Invoke($"give {id}");
                    MessageBox.Show($"Sent command: give {id}", "Swaffle Console");
                }
                else
                {
                    MessageBox.Show("Item ID not found", "Error");
                }
            }
            else
            {
                MessageBox.Show("Please select an actual item.", "Info");
            }
        }

        private void AssetTree_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(AssetTree.SelectedItem is TreeViewItem selected)
            {
                string name = selected.Header.ToString().Replace("★ ", "");
                if(favorites.Contains(name))
                    favorites.Remove(name);
                else
                    favorites.Add(name);

                SaveFavorites();
                RefreshItems(itemLookup);
            }
        }

        private void LoadFavorites()
        {
            if(File.Exists(favoritesFile))
                favorites = new HashSet<string>(File.ReadAllLines(favoritesFile));
        }

        private void SaveFavorites()
        {
            File.WriteAllLines(favoritesFile, favorites);

        }
    }
    }
