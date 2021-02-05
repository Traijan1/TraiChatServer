using System;
using System.Collections.Generic;
using System.Text;

namespace TraiChatServer {
    class ChatManager {
        static List<Chat> chats = new List<Chat>();

        public static List<Chat> Chats { get { return chats; } private set { chats = value; } }
        public static Chat Primary { 
            get {
                return chats.Find(c => c.Primary == true);
            } 
        }

        /// <summary>
        /// Erstellt einen neuen Chat und sendet allen Online Usern den neuen Chat
        /// </summary>
        /// <param name="name">Der Name des Chats</param>
        /// <param name="desc">Die Beschreibung des Chats</param>
        /// <returns>TRUE wenn der Chat erstellt werden konnte, FALSE wenn nicht</returns>
        public static bool CreateChat(String name, String desc = "", bool primary = false) {
            String id = Database.CreateChat(name, desc, primary);
            var chat = new Chat(id, name, desc, primary);
            Chats.Add(chat);

            // Send to all users that chat is created
            SocketMessage sm = new SocketMessage(MessageType.ChatWasCreated);
            sm.AddHeaderData("chatID", chat.ID);
            sm.AddHeaderData("chatName", chat.Name);
            sm.AddHeaderData("chatDesc", chat.Description);
            ClientManager.Broadcast(sm);

            return true;
        }

        /// <summary>
        /// Fügt ein Chat in die chats-Liste hinzu
        /// </summary>
        /// <param name="chat">Das Chat-Objekt das hinzugefügt werden soll</param>
        public static void AddChat(Chat chat) =>
            chats.Add(chat);

        public static Chat FindById(String id) {
            return chats.Find(c => c.ID == id);
        }
    }
}
