using System;
using System.Collections.Generic;
using System.Text;
using Cassandra;

namespace TraiChatServer {
    static class Database { // create table messages (id uuid, time timestamp, message text, file text, reply uuid, edited boolean, uid uuid, PRIMARY KEY(uid, time, id)) WITH CLUSTERING ORDER BY (time DESC);
        static String ip = "192.168.178.65";
        static String keySpace = "test_messages";
        static String messageTable = "messages";
        static String sortedMessages = "messages_by_time";

        static ISession session;

        /// <summary>
        /// Verbindet sich mit der Datenbank
        /// </summary>
        public static void Connect() {
            var cluster = Cluster.Builder()
                .AddContactPoint(ip).WithPort(9042).WithCredentials("cassandra", "cassandra").Build();

            session = cluster.Connect(keySpace);
        }

        /// <summary>
        /// Erstellt alle notwendigen Tabellen
        /// </summary>
        public static void CreateTables() { // Muss getestet werden
            session.Execute("CREATE TABLE messages (id uuid, message text, file text, reply uuid, edited boolean, uid uuid, chat uuid, PRIMARY KEY(id, chat));");
            session.Execute("CREATE TABLE messages_by_time (message_id uuid, chat uuid, time timeStamp, PRIMARY KEY(message_id, time)) WITH CLUSTERING ORDER BY (time ASC);"); // DESC oder ASC?
            // Chat Table
            // User Table
        }

        /// <summary>
        /// Fügt eine Nachricht der messages-Tabelle hinu
        /// </summary>
        /// <param name="mes">Die Nachricht selber</param>
        /// <param name="file">Pfad zu einer Datei</param>
        /// <param name="uid">Die ClientID</param>
        /// <param name="chatID">Die ChatID</param>
        /// <param name="reply">Die ReplyID</param>
        /// <returns>Die ID der Nachricht</returns>
        public static String AddMessage(String mes, String file, String uid, String chatID, String reply) {
            Guid uuid = Guid.NewGuid();
            String replyID = reply == "" ? Guid.Empty.ToString() : reply; // Schauen was als reply-ID eingesetzt werden soll

            session.Execute($"INSERT INTO {messageTable} " +
                    "(id, message, file, reply, edited, uid, chat) values (" + uuid +
                    ", '" + mes + "'" +
                    ", '" + file + "'" +
                    ", " + replyID +
                    ", " + false +
                    ", " + Guid.Parse(uid) +
                    ", " + chatID +
                ");");

            // In die sortierte Messages Tabelle eintragen
            session.Execute($"INSERT INTO {sortedMessages} (message_id, chat, time) VALUES ({uuid}, {chatID}, toTimestamp(now()));");

            return uuid.ToString();
        }

        /// <summary>
        /// Holt sich die Nachrichten aus der Datenbank
        /// </summary>
        /// <param name="chatId">Die ChatID inwelcher die Nachrichten gesucht werden</param>
        /// <param name="limitVal">Wie viele Nachrichten auf einen Rutsch rausgesucht werden</param>
        /// <returns>Eine Liste aus ChatMessages</returns>
        public static List<ChatMessage> GetMessages(String chatId, int limitVal = 50) { // NOCH DATEIEN EINBAUEN, Schauen ob es auch 50 Nachrichten zuschicken kann
            var messagesSorted = session.Execute($"SELECT message_id FROM {sortedMessages} WHERE chat = {chatId} LIMIT {limitVal} ALLOW FILTERING");

            List<ChatMessage> list = new List<ChatMessage>();

            foreach (var row in messagesSorted) {
                String messageId = row.GetValue<Guid>("message_id").ToString();

                var messageRow = session.Execute($"SELECT message, file, reply, edited, uid FROM {messageTable} WHERE id = {messageId}"); // Maybe ALLOW FILTERING?
                list.Add(GetMessageById(messageId, true));
            }

            return list;
        }

        /// <summary>
        /// Sucht die Message nach der ID raus
        /// </summary>
        /// <param name="messageID">Die ID der Message</param>
        /// <returns>Die Message als ChatMessage</returns>
        public static ChatMessage GetMessageById(String messageID, bool getReply = false) { // maybe nur ID returnen, je nachdem wie ich es weiter aufbauen will
            var messageRow = session.Execute($"SELECT message, file, reply, edited, uid FROM {messageTable} WHERE id = {messageID}");

            ChatMessage chatMessage = null;

            foreach(var mes in messageRow) {
                String uid = mes.GetValue<Guid>("uid").ToString();
                String username = GetUsername(uid); 
                String message = mes.GetValue<String>("message");
                String filePath = mes.GetValue<String>("file");
                bool wasEdited = mes.GetValue<bool>("edited");
                Guid replyID = mes.GetValue<Guid>("reply");

                var timeRow = session.Execute($"SELECT time FROM {sortedMessages} WHERE message_id = {messageID};");
                DateTime time = DateTime.MinValue;

                foreach(var row in timeRow)
                    time = row.GetValue<DateTime>("time");

                String reply = "";
                if(replyID != Guid.Empty && getReply) // Wenn eine Reply-ID existiert und eine Reply gesucht wird mit, dann such sie.
                    reply = GetMessageById(replyID.ToString()).Message;

                chatMessage = new ChatMessage(username, uid, message, filePath, messageID, time, reply, wasEdited);
            }

            return chatMessage;
        }

        /// <summary>
        /// Updatet eine Nachricht
        /// </summary>
        /// <param name="id">Die Message-ID</param>
        /// <param name="chatID">Die Chat-ID</param>
        /// <param name="newMessage">Die neue Nachricht</param>
        public static void UpdateMessage(String id, String chatID, String newMessage) {
            session.Execute($"UPDATE {messageTable} SET message = '{newMessage}' WHERE id = {id} AND chat = {chatID} IF EXISTS");
            session.Execute($"UPDATE {messageTable} SET edited = true WHERE id = {id} AND chat = {chatID} IF EXISTS");
        }

        /// <summary>
        /// Löscht eine Nachricht aus der Datenbank
        /// </summary>
        /// <param name="messageID">Die ID der Nachricht die gelöscht werden soll</param>
        /// <returns>Die Chat-ID, inwelcher die Nachricht war</returns>
        public static String DeleteMessage(String messageID) {
            var rows = session.Execute($"DELETE FROM {sortedMessages} WHERE message_id = {messageID}");

            rows = session.Execute("SELECT chat FROM messages WHERE id = " + messageID);
            session.Execute("DELETE FROM messages WHERE id = " + messageID);
            // Hier noch updaten damit, wenn eine Nachricht diese Nachricht als reply hat auf Guid.Empty gesetzt wird, maybe bei messages noch ein "reply-deletet" hinzufügen, damit man anzeigen kann das die reply gelöscht wurde

            foreach(var row in rows)
                return row.GetValue<Guid>("chat").ToString();

            throw new Exception("Die Nachricht existierte nicht");
        }

        /// <summary>
        /// Erstellt einen Chat in der Datenbank
        /// </summary>
        /// <param name="name">Der Name des Chats</param>
        /// <param name="desc">Die Beschreibung des Chats</param>
        /// <param name="primary">Ob der Chat ein primary-Chat ist</param>
        /// <returns>Die ID des Chats</returns>
        public static String CreateChat(String name, String desc = "", bool primary = false) {
            Guid uuid = Guid.NewGuid();

            session.Execute("INSERT INTO chat (id, name, description, icon, primaryChat, created) VALUES (" +
                    uuid + 
                    ", '" + name + "'" +
                    ", '" + desc + "'" +
                    ", '" + "' ," + // Chat Icon später hinzufügen
                    primary + ", " +
                    "toTimestamp(now())" +
                 ");") ;

            return uuid.ToString();
        }

        /// <summary>
        /// Sucht alle Chats raus und erstellt diese als Objekte
        /// </summary>
        public static void GetChats() {
            var result = session.Execute("SELECT * FROM chat");

            foreach(var row in result) {
                String id = row.GetValue<Guid>("id").ToString();
                String name = row.GetValue<String>("name");
                String desc = row.GetValue<String>("description");
                bool primary = row.GetValue<bool>("primarychat");

                Chat cache = new Chat(id, name, desc, primary);
                ChatManager.AddChat(cache);
            }
        }

        /// <summary> => Methode wird entfernt, ist nur zum testen da
        /// Sucht die ClientID anhand der E-Mail
        /// </summary>
        /// <param name="email">Die E-Mail des Clients</param>
        /// <returns>Díe ClientID</returns>
        public static String GetUID(String email) { 
            var uid = session.Execute("SELECT uid FROM users where email = '" + email + "' ALLOW FILTERING;"); // Nach besseren Weg überlegen

            foreach(var id in uid)
                return id.GetValue<Guid>("uid").ToString();

            return "";
        }

        /// <summary>
        /// Sucht den Username anhand der ClientID
        /// </summary>
        /// <param name="uid">Die ClientID</param>
        /// <returns>Den Username</returns>
        public static String GetUsername(String uid) {
            var uids = session.Execute("SELECT username FROM users where uid = " + uid + ";"); 

            foreach(var id in uids)
                return id.GetValue<String>("username");

            return "";
        }
    }
}
