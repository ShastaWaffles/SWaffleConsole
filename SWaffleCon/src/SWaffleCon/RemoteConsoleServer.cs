using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zorro.Core;

namespace SWaffleCon
{
    public class RemoteConsoleServer : MonoBehaviour
    {
        private TcpListener? listener;
        private static readonly List<TcpClient> connectedClients = new();

        private void Start()
        {
            Application.logMessageReceived += HandleUnityLog;

            listener = new TcpListener(IPAddress.Loopback, 7777);
            listener.Start();
            Debug.Log("Remote Terminal listening on port 7777");

            _ = Task.Run(() => ListenLoop());


        }

        private async Task ListenLoop()
        {
            while (true)
            {
                var client = await listener!.AcceptTcpClientAsync();
                lock (connectedClients)
                    connectedClients.Add(client);
                _ = Task.Run(() => HandleClient(client));
            }

        }

        private static void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            string message = $"[{type}] {logString}";
            BroadcastToClients(message);
        }
        private async void HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Debug.Log("[RemoteConsole] Command Received: " + line);

                try
                {
                    Plugin.mainThreadActions.Enqueue(() =>
                    {
                        CommandHandler.Execute(line);

                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RemoteConsole] Command Crash: " + ex);
                }
            }

            Debug.Log($"[RemoteCMD] {line}");
        }

        private static void BroadcastToClients(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            lock (connectedClients)
            {
                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        NetworkStream stream = connectedClients[i].GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        connectedClients.RemoveAt(i);
                    }
                }
            }
        }
        public static void SendItemListToClients()
        {

            var items = SingletonAsset<ItemDatabase>.Instance.itemLookup;
            BroadcastToClients("[ItemsListStart]");
            foreach (var pair in items)
            {
                BroadcastToClients($"[Item] {pair.Key}|{pair.Value.name}");
            }
            BroadcastToClients("[ItemsListEnd]");

        }
    }

}
