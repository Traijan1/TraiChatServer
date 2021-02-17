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
            LogManager.LogStartup("Verbindung zur Datenbank herstellen...");
            Database.Connect();
            LogManager.LogStartup("Erfolgreich eine Verbindung hergestellt");

            // Chats laden
            LogManager.LogStartup("Chats werden geladen...");
            Database.GetChats();
            LogManager.LogStartup("Chats wurden erfolgreich geladen");

            // Konfiguration auslesen (iwann in Datenbank ausweiten, oder Config File (je nachdem wo man die Sachen ändern soll))
            LogManager.LogStartup("Auslesen der Konfigurationsdatei...");
            _name = "Traijan's Verrückter Server";
            buf = new byte[4096];
            LogManager.LogStartup("Erfolgreich ausgelesen");

            // Server starten
            LogManager.LogStartup("Server starten...");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(2); // 2 gleichzeitig zum accepten, alle andere in einer Warteschlange
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            LogManager.LogStartup("Erfolgreich Server gestartet");
        }

        static void FirstInit() {
            ChatManager.CreateChat("Welcome", primary: true); // 2 Parameter mit Default values, mit : kann man entscheiden welchen man setzen will
        }

        static void AcceptCallback(IAsyncResult ar) {
            Socket socket = serverSocket.EndAccept(ar);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            socket.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ReceiveCallback, socket);

            var message = new SocketMessage(MessageType.Verify);
            message.AddHeaderData("serverName", _name); // Send things like Server Name, Online Users, etc
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
                    LogManager.LogError("Konnte Client nicht disconnecten");
                else
                    LogManager.LogConnection(name + " hat die Verbindung verloren");

                return;
            }

            byte[] data = new byte[recv];
            Array.Copy(buf, data, recv);

            SocketMessage m = SocketMessage.FromJSONBytes(data);

            switch(m.MessageType) {
                case MessageType.Verify:
                    AddClient(m, sock);
                    SendChats(sock);
                    System.Threading.Thread.Sleep(100); // Schauen ob man das iwie loswerden kann
                    SendPrimaryChat(sock);
                    break;
                case MessageType.Disconnect:
                    ClientManager.DisconnectClient(sock, out string name);
                    LogManager.LogConnection($"{name} hat die Verbindung getrennt");

                    return; // Just dont begin to receive new data
                case MessageType.Message:
                    if(m.Header["chatID"] == "")
                        throw new Exception("ChatID von Client war leer");

                    String replyID = m.Header["reply"];

                    String uid = ClientManager.FindBySocket(sock).ID;
                    String messageId = Database.AddMessage(m.Header["message"], m.Header["filePath"], uid, m.Header["chatID"], replyID);

                    String replyMessage = "";

                    if(replyID != Guid.Empty.ToString())
                        replyMessage = Database.GetMessageById(replyID).Message;

                    foreach(Client c in ChatManager.FindById(m.Header["chatID"]).Users) // So umbauen das der Chat.Broadcast() hier angewendet wird
                        c.SendMessage(ClientManager.FindBySocket(sock), m.Header["message"], "", messageId, replyMessage);

                    break;
                case MessageType.JoinChat:
                    String chatID = m.Header["chatID"];
                    Client client = ClientManager.FindBySocket(sock);
                    Chat chat = ChatManager.FindById(chatID);

                    if(client == null) {
                        LogManager.LogError("Client konnte nicht gefunden werden: JoinChat-Event");
                        return;
                    }

                    if(chat == null) {
                        LogManager.LogError("Chat konnte nicht gefunden werden: JoinChat-Event");
                        return;
                    }

                    chat.Join(client);

                    // Dem Client bestätigen das er joinen darf
                    sock.Send(JoinChatMessage(chat).ToJSONBytes());                    

                    // Nachrichten aus dem Chat schicken
                    var sm = new SocketMessage(MessageType.ChatContent);
                    var messageList = Database.GetMessages(chatID);

                    LogManager.LogChatEvent($"{client.Name} ({client.ID}) hat den Chat: {chat.Name} ({chat.ID}) betreten");

                    sm.AddHeaderData("messages", JsonConvert.SerializeObject(messageList));
                    sock.Send(sm.ToJSONBytes());
                    break;
                case MessageType.DeleteMessage:
                    String messageID = m.Header["messageID"];
                    chatID = Database.DeleteMessage(messageID);

                    var message = new SocketMessage(MessageType.DeleteMessage);
                    message.AddHeaderData("messageID", messageID);

                    ChatManager.FindById(chatID).Broadcast(message);
                    break;
            }

            sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ReceiveCallback, sock);
        }

        public static void AddClient(SocketMessage m, Socket socket) {
            // Zuerst hinzufügen
            Client c = new Client(m.Header["email"], socket); // Über Client ID bekommen statt durch Mail

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

            // Neuen Client allen Onlineuser + sich selbst zuschicken
            socketMessage = new SocketMessage(MessageType.SendVariousInformations);
            socketMessage.AddHeaderData("users", JsonConvert.SerializeObject(users));
            socketMessage.AddHeaderData("id", c.ID);
            socket.Send(socketMessage.ToJSONBytes());

            LogManager.LogConnection("Client besitzt die UID: " + c.ID + " mit dem Namen: " + c.Name);

            // Primary Chat zuweisen
            ChatManager.Primary.Join(c);
        }

        /// <summary>
        /// Gibt dem Client die Chat Daten zu dem er gerade joint
        /// </summary>
        /// <param name="chat">Der Chat der gejoint wird</param>
        /// <returns></returns>
        static SocketMessage JoinChatMessage(Chat chat) {
            var sm = new SocketMessage(MessageType.JoinChat);
            sm.AddHeaderData("id", chat.ID);
            sm.AddHeaderData("name", chat.Name);
            sm.AddHeaderData("desc", chat.Description);

            return sm;
        }

        /// <summary>
        /// Sendet den Primary-Chat an den neu verbundenen Client
        /// </summary>
        /// <param name="socket">Der neu verbundene Client</param>
        public static void SendPrimaryChat(Socket socket) {
            // Primary Chat suchen und zuschicken
            Chat chat = ChatManager.Chats.Find(c => c.Primary == true);

            socket.Send(JoinChatMessage(chat).ToJSONBytes());

            System.Threading.Thread.Sleep(100); // Schauen ob man das iwie loswerden kann

            // Inhalte des Primary Chats zusenden
            var sm = new SocketMessage(MessageType.ChatContent);
            sm.AddHeaderData("messages", JsonConvert.SerializeObject(Database.GetMessages(chat.ID)));
            socket.Send(sm.ToJSONBytes());
        }

        /// <summary>
        /// Sendet alle Chats an den neu eingeloggten Client
        /// </summary>
        /// <param name="socket">Der neue Client</param>
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
