using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace TraiChatServer {
    static class ClientManager {
        static List<Client> online = new List<Client>();
        public static List<Client> Online { get { return online; } }

        public static void Add(Client cl) {
            online.Add(cl);
        }

        public static Client FindByName(String id) {
            return online.Find(client => client.ID == id);
        }

        public static Client FindBySocket(Socket socket) {
            return online.Find(client => client.Socket == socket);
        }

        public static bool DisconnectClient(Socket socket, out string name) {
            Client client = FindBySocket(socket);

            if(!online.Remove(client)) {
                name = "";
                return false;
            }

            name = client.Name;

            // Disconnect from chat + send a chat message that x disconnected
            // If a Userlist exists, update it

            return true;
        }
    }
}
