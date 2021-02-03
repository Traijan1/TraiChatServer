using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace TraiChatServer {
    static class ClientManager {
        static List<Client> online = new List<Client>();
        public static List<Client> Online { get { return online; } }

        /// <summary>
        /// Fügt einen Client in die Onlineliste hinzu
        /// </summary>
        /// <param name="cl">Der neue Client</param>
        public static void Add(Client cl) {
            online.Add(cl);
        }

        /// <summary>
        /// Lässt einen Client über die ID finden
        /// </summary>
        /// <param name="id">Die ID des gesuchten Clients</param>
        /// <returns>Den Client, falls nicht gefunden: null</returns>
        public static Client FindByID(String id) {
            return online.Find(client => client.ID == id);
        }

        /// <summary>
        /// Lässt einen client über den Socket finden
        /// </summary>
        /// <param name="socket">Der Socket des gesuchten Clients</param>
        /// <returns>Den Client, falls nicht gefunden: null</returns>
        public static Client FindBySocket(Socket socket) {
            return online.Find(client => client.Socket == socket);
        }

        /// <summary>
        /// Sendet allen Client die Online sind eine Nachricht
        /// </summary>
        /// <param name="message">Die Nachricht die gesendet werden soll</param>
        public static void Broadcast(SocketMessage message) {
            foreach(var c in Online)
                c.Send(message);
        }

        /// <summary>
        /// Schließt die Verbindung zu einem Client sauber
        /// </summary>
        /// <param name="socket">Der Socket der geschlossen werden soll</param>
        /// <param name="name">Der Name des Clients</param>
        /// <returns>TRUE wenn die Verbindung sauber geschlossen werden konnte, FALSE wenn es zu Problemen kam</returns>
        public static bool DisconnectClient(Socket socket, out string name) {
            Client client = FindBySocket(socket);

            if(!online.Remove(client)) {
                name = "";
                return false;
            }

            name = client.Name;
            SocketMessage clientDisconnectMessage = new SocketMessage(MessageType.OtherClientDisconnect);
            clientDisconnectMessage.AddHeaderData("id", client.ID);
            Broadcast(clientDisconnectMessage);

            // If a Userlist exists, update it

            return true;
        }
    }
}
