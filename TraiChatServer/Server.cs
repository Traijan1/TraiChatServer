using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
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
            // Datenbankverbindung aufbauen
            Console.WriteLine("[STARTUP] Verbindung zur Datenbank herstellen...");
            Database.Connect();
            Console.WriteLine("[STARTUP] Erfolgreich eine Verbindung hergestellt");

            // Chats laden
            Console.WriteLine("[STARTUP] Chats werden geladen...");
            Database.GetChats();
            Console.WriteLine("[STARTUP] Chats wurden erfolgreich geladen");

            // Konfiguration auslesen (iwann in Datenbank ausweiten, oder Config File (je nachdem wo man die Sachen ändern soll))
            Console.WriteLine("[STARTUP] Auslesen der Konfigurationsdatei...");
            _name = "Traijan's Verrückter Server";
            buf = new byte[4096];
            Console.WriteLine("[STARTUP] Erfolgreich ausgelesen");

            // Server starten
            Console.WriteLine("[STARTUP] Server starten...");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(2); // 2 gleichzeitig zum accepten, alle andere in einer Warteschlange
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            Console.WriteLine("[STARTUP] Erfolgreich Server gestartet");
        }

        static void FirstInit() {
            ChatManager.CreateChat("Welcome", primary: true);
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
                    SendChats(sock);
                    break;
                case MessageType.Disconnect:
                    ClientManager.DisconnectClient(sock, out string name);
                    Console.WriteLine("[CONNECTION] " + name + " hat die Verbindung getrennt");

                    return; // Just dont begin to receive new data
                case MessageType.Message:
                    String uid = ClientManager.FindBySocket(sock).ID;
                    String messageId = Database.AddMessage(m.Header["message"], m.Header["filePath"], uid);
                    
                    foreach(Client c in ClientManager.Online)
                        c.SendMessage(ClientManager.FindBySocket(sock), m.Header["message"], "", messageId, "");

                    break;
                case MessageType.JoinChat:
                    var socketMessage = new SocketMessage(MessageType.ChatContent);
                    var messageList = Database.GetMessages();

                    Console.WriteLine("[CHAT JOIN] Es wurde einem Chat beigetreten");

                    socketMessage.AddHeaderData("messages", JsonConvert.SerializeObject(messageList));
                    sock.Send(socketMessage.ToJSONBytes());
                    break;
            }

            sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ReceiveCallback, sock);
        }

        public static void AddClient(SocketMessage m, Socket socket) {
            // Zuerst hinzufügen
            Client c = new Client(m.Header["email"], socket);

            // Allen anderen eine Nachricht schicken das der neue Client gejoint ist
            var socketMessage = new SocketMessage(MessageType.NewUserConnected);
            socketMessage.AddHeaderData("name", c.Name);
            socketMessage.AddHeaderData("id", c.ID);
            ClientManager.Broadcast(socketMessage);

            // Client einbinden in den ClientManager
            ClientManager.Add(c);

            // Schauen wie man beim Client die Userdata sendet (also was alles, mindestens UserID und Name) Maybe mit der Lösung jetzt =>
            List<Dictionary<String, String>> users = new List<Dictionary<string, string>>();

            foreach(var s in ClientManager.Online) {
                Dictionary<String, String> cache = new Dictionary<string, string>();
                cache.Add("name", s.Name);
                cache.Add("id", s.ID);
                users.Add(cache);
            }

            // Neuen Client alle Onlineuser + sich selbst zuschicken
            socketMessage = new SocketMessage(MessageType.SendOnlineUsers);
            socketMessage.AddHeaderData("users", JsonConvert.SerializeObject(users));
            socket.Send(socketMessage.ToJSONBytes());

            Console.WriteLine("[CONNECTION] Client besitzt die UID: " + c.ID + " mit dem Namen: " + c.Name);
        }

        public static void SendChats(Socket socket) {
            var sm = new SocketMessage(MessageType.GetChats);
            List<Dictionary<String, String>> chats = new List<Dictionary<String, String>>();

            foreach(var c in ChatManager.Chats) {
                Dictionary<String, String> cache = new Dictionary<string, string>();

                cache.Add("name", c.Name);
                cache.Add("desc", c.Description);
                cache.Add("id", c.ID);

                chats.Add(cache);
            }

            String json = JsonConvert.SerializeObject(chats);

            sm.AddHeaderData("chats", json);
            socket.Send(sm.ToJSONBytes());
        }
    }
}
