using System.ComponentModel.Design;
using UnityEngine;
using Zorro.Core;

namespace SWaffleCon
{
    public static class CommandHandler
    {
        public static void Execute(string input)
        {
            Debug.Log($"[RemoteConsole] Executing command: {input}");

            string[] parts = input.Split(' ');
            string cmd = parts[0].ToLower();
            string[] args = parts.Length > 1 ? parts[1..] : new string[0];


            switch (cmd)
            {
                // Just a basic comamnd. 
                case "hello":
                    Debug.Log("Hello from external terminal");
                    break;

                // listitems command to have the game send over a list of items in the item database by <id>|<name>. This command is ran when the asset browser is first opened. 
                case "listitems":
                    Debug.Log("[RemoteConsole] Sending item list to External Console...");
                    RemoteConsoleServer.SendItemListToClients();
                    break;

                // finditem command to search for items and thier ID. Not really necessary. Unless you never open the asset browser. 
                case "finditem":
                    if (args.Length < 1)
                    {
                        Debug.Log("[Remote Console] Usage: finditem <itemName>");
                        return;
                    }
                    string searchName = string.Join(" ", args).ToLower();
                    var lookup = SingletonAsset<ItemDatabase>.Instance.itemLookup;
                    bool found = false;

                    foreach (var kvp in lookup)
                    {
                        if (kvp.Value.name.ToLower().Contains(searchName))
                        {
                            Debug.Log($"[RemoteConsole] Match: ID={kvp.Key} Name = {kvp.Value.name}");
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        Debug.Log($"[RemoteConsole] No Items found matching: {searchName}");
                    }
                    break;

                // a basic give command once you have the ID from the find item command. Asset browser does this on selection and button press. 

                case "give":
                    Debug.Log($"[RemoteConsole] give command with Args: {string.Join(", ", args)}");
                    if (args.Length < 1)
                    {
                        Debug.Log($"[RemoteConsole] Usage: give <itemID>");
                        return;
                    }

                    if (ushort.TryParse(args[0], out ushort itemID))
                    {
                        Debug.Log($"[RemoteConsole] Trying item ID: {itemID}");

                        if (ItemDatabase.TryGetItem(itemID, out var item))
                        {
                            Debug.Log($"[RemoteConsole] Found Item: {item.name}");
                            ItemDatabase.Add(item);
                            Debug.Log($"[RemoteConsole] Item added: {item.name}");
                        }
                        else
                        {
                            Debug.Log($"[RemoteConsole] Item ID {itemID} not found in database");
                        }
                    }
                    else
                    {
                        Debug.Log("RemoteConsole] Could not parse item ID.");
                    }

                    break;

                default:
                    Debug.Log($"[RemoteConsole] Unknown Command: {cmd}");
                    break;
            }
        }
    }
}
