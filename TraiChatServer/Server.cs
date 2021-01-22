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
        static byte[] buf;

        public static String Name { get { return _name; } }

        public static void InitServer() {
            _name = "Traijan's Verrückter Server";
            buf = new byte[4096];
            _connectedClients = new List<Client>();
            
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
            socket.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ReceiveCallback, socket);

            var message = new SocketMessage(MessageType.Verify);
            message.AddHeaderData("name", _name); // Send things like Server Name, Online Users, etc
            socket.Send(message.ToJSONBytes());

            //AddClient(new Client());
        }

        static void ReceiveCallback(IAsyncResult ar) {
            Socket sock = (Socket)ar.AsyncState;
            int recv = 0;

            try {
                recv = sock.EndReceive(ar); 
            }
            catch {
                // Find Client with sock als Socket and disconnect him properly
                return;
            }


            sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ReceiveCallback, sock);

            byte[] data = new byte[recv];
            Array.Copy(buf, data, recv);

            SocketMessage m = SocketMessage.FromJSONBytes(data);

            switch(m.MessageType) {
                case MessageType.Verify:
                    AddClient(m);
                    break;
            }
        }

        public static void AddClient(SocketMessage m) {
            _connectedClients.Add(new Client());

            Console.WriteLine("[Neue Verbindung] Client besitzt die ID: " + m.Header["id"] + " mit dem Namen: " + m.Header["name"]);
        }
    }
}
