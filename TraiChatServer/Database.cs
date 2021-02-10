using System;
using System.Collections.Generic;
using System.Text;
using Cassandra;

namespace TraiChatServer {
    static class Database { // create table messages (id uuid, time timestamp, message text, file text, reply uuid, edited boolean, uid uuid, PRIMARY KEY(uid, time, id)) WITH CLUSTERING ORDER BY (time DESC);
        static String ip = "192.168.178.65";
        static String keySpace = "test_messages";

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

            session.Execute("INSERT INTO messages " +
                    "(id, time, message, file, reply, edited, uid, chat) values (" + uuid +
                    ", toTimestamp(now())" +
                    ", '" + mes + "'" +
                    ", '" + file + "'" +
                    ", " + replyID +
                    ", " + false +
                    ", " + Guid.Parse(uid) +
                    ", " + chatID +
                ");");

            return uuid.ToString();
        }

        /// <summary>
        /// Holt sich die Nachrichten aus der Datenbank
        /// </summary>
        /// <param name="chatId">Die ChatID inwelcher die Nachrichten gesucht werden</param>
        /// <param name="limitVal">Wie viele Nachrichten auf einen Rutsch rausgesucht werden</param>
        /// <returns>Eine Liste aus ChatMessages</returns>
        public static List<ChatMessage> GetMessages(String chatId, int limitVal = 50) { // NOCH DATEIEN EINBAUEN, Schauen ob es auch 50 Nachrichten zuschicken kann
            var messages = session.Execute("SELECT id, time, message, file, reply, edited, uid FROM messages WHERE chat = " + chatId + " LIMIT " + limitVal + " ALLOW FILTERING;");

            List<ChatMessage> list = new List<ChatMessage>();

            foreach(var mes in messages) {
                String uid = mes.GetValue<Guid>("uid").ToString();
                String username = GetUsername(uid); // ClientManager.FindByID() funktioniert nicht da der User auch offline sein kann
                String message = mes.GetValue<String>("message");
                DateTime time = mes.GetValue<DateTime>("time");
                String filePath = mes.GetValue<String>("file");
                Guid replyID = mes.GetValue<Guid>("reply");

                String reply = "";
                if(replyID != Guid.Empty)
                    reply = GetReplyMessage(replyID.ToString()).Message;

                bool wasEdited = mes.GetValue<bool>("edited");
                String messageId = mes.GetValue<Guid>("id").ToString();

                list.Add(new ChatMessage(username, uid, message, filePath, messageId, time, reply, wasEdited));
            }

            return list;
        }

        /// <summary>
        /// Sucht die Reply-Message raus
        /// </summary>
        /// <param name="messageID">Die ID der Reply-Message</param>
        /// <returns>Die Reply-Message als ChatMessage</returns>
        public static ChatMessage GetReplyMessage(String messageID) { // maybe nur ID returnen, je nachdem wie ich es weiter aufbauen will
            var messages = session.Execute("SELECT id, time, message, file, edited, uid FROM messages where id = " + messageID + " and time < toTimestamp(now()) ALLOW FILTERING;");

            ChatMessage chatMessage = null;

            foreach(var mes in messages) {
                String uid = mes.GetValue<Guid>("uid").ToString();
                String username = GetUsername(uid); 
                String message = mes.GetValue<String>("message");
                DateTime time = mes.GetValue<DateTime>("time");
                String filePath = mes.GetValue<String>("file");
                bool wasEdited = mes.GetValue<bool>("edited");
                String messageId = mes.GetValue<Guid>("id").ToString();

                chatMessage = new ChatMessage(username, uid, message, filePath, messageId, time, "", wasEdited);
            }

            return chatMessage;
        }


        /// <summary>
        /// Löscht eine Nachricht aus der Datenbank
        /// </summary>
        /// <param name="messageID">Die ID der Nachricht die gelöscht werden soll</param>
        /// <returns>Die Chat-ID, inwelcher die Nachricht war</returns>
        public static String DeleteMessage(String messageID) {
            var rows = session.Execute("SELECT chat FROM messages WHERE id = " + messageID);
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
