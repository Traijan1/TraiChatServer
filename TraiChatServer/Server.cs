using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TraiChatServer {
    static class Server {
        static String _name;
        static List<Client> _connectedClients;
        static List<Chat> _chats;

        static Socket serverSocket;
        const int port = 7777;

        public static String Name { get { return _name; } }

        public static void InitServer() {
            // Server starten
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(2); // 2 gleichzeitig zum accepten, alle andere in einer Warteschlange
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null); 

            // Chats laden
            LoadChats();
        }

        public static void LoadChats() {

        }

        static void AcceptCallback(IAsyncResult ar) {
            Socket socket = serverSocket.EndAccept(ar);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

            Console.WriteLine("Socket: " + socket.ToString() + " hat sich verbunden");
            //AddClient(new Client());
        }

        public static void AddClient(Client client) {
            _connectedClients.Add(client);
        }
    }
}
