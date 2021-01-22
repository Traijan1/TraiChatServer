using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TraiChatServer {
    static class Server {
        static String _name;
        static List<Chat> _chats;

        static Socket serverSocket;
        const int port = 7777;
        static byte[] buf;

        public static String Name { get { return _name; } }

        public static void InitServer() {
            _name = "Traijan's Verrückter Server";
            buf = new byte[4096];
            
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
        }

        static void ReceiveCallback(IAsyncResult ar) {
            Socket sock = (Socket)ar.AsyncState;
            int recv = 0;

            try {
                recv = sock.EndReceive(ar); 
            }
            catch {
                if(!ClientManager.DisconnectClient(sock, out string name))
                    Console.WriteLine("[ERROR] Konnte Client nicht disconnecten");
                else
                    Console.WriteLine("[CONNECTION] " + name + " hat die Verbindung verloren");

                return;
            }

            byte[] data = new byte[recv];
            Array.Copy(buf, data, recv);

            SocketMessage m = SocketMessage.FromJSONBytes(data);

            switch(m.MessageType) {
                case MessageType.Verify:
                    AddClient(m, sock);
                    break;
                case MessageType.Disconnect:
                    ClientManager.DisconnectClient(sock, out string name);
                    Console.WriteLine("[CONNECTION] " + name + " hat die Verbindung getrennt");
                    return; // Just dont begin to receive new data
            }

            sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ReceiveCallback, sock);
        }

        public static void AddClient(SocketMessage m, Socket socket) {
            ClientManager.Add(new Client(m.Header["name"], m.Header["id"], socket));
            // Maybe think about a message to all users in chat
            // show in list (if some list exists in gui)

            Console.WriteLine("[CONNECTION] Client besitzt die ID: " + m.Header["id"] + " mit dem Namen: " + m.Header["name"]);
        }
    }
}
