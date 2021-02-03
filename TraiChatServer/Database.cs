using System;
using System.Collections.Generic;
using System.Text;
using Cassandra;

namespace TraiChatServer {
    static class Database { // create table messages (id uuid, time timestamp, message text, file text, reply uuid, edited boolean, uid uuid, PRIMARY KEY(uid, time, id)) WITH CLUSTERING ORDER BY (time DESC);
        static String ip = "192.168.178.65";
        static String keySpace = "test_messages";

        static ISession session;

        public static void Connect() {
            var cluster = Cluster.Builder()
                .AddContactPoint(ip).WithPort(9042).WithCredentials("cassandra", "cassandra").Build();

            session = cluster.Connect(keySpace);
        }

        public static String AddMessage(String mes, String file, String uid) {
            Guid uuid = Guid.NewGuid();

            session.Execute("INSERT INTO messages " +
                    "(id, time, message, file, reply, edited, uid) values (" + uuid +
                    ", toTimestamp(now())" +
                    ", '" + mes + "'" +
                    ", '" + file + "'" +
                    ", " + Guid.Empty +
                    ", " + false +
                    ", " + Guid.Parse(uid) +
                ");");

            return uuid.ToString();
        }

        public static List<ChatMessage> GetMessages(int limitVal = 50) { // NOCH DATEIEN EINBAUEN
            var messages = session.Execute("SELECT id, time, message, file, reply, edited, uid FROM messages LIMIT " + limitVal + ";");

            List<ChatMessage> list = new List<ChatMessage>();

            foreach(var mes in messages) {
                String username = GetUsername(mes.GetValue<Guid>("uid").ToString()); // ClientManager.FindByID() funktioniert nicht da der User auch offline sein kann
                String message = mes.GetValue<String>("message");
                DateTime time = mes.GetValue<DateTime>("time");
                String filePath = mes.GetValue<String>("file");
                Guid replyID = mes.GetValue<Guid>("reply");

                String reply = "";
                if(replyID != Guid.Empty)
                    reply = GetReplyMessage(replyID.ToString()).Message;

                bool wasEdited = mes.GetValue<bool>("edited");
                String messageId = mes.GetValue<Guid>("id").ToString();

                list.Add(new ChatMessage(username, message, filePath, messageId, time, reply, wasEdited));
            }

            return list;
        }

        public static ChatMessage GetReplyMessage(String messageID) {
            var messages = session.Execute("SELECT id, time, message, file, edited, uid FROM messages where id = " + messageID + " and time < toTimestamp(now()) ALLOW FILTERING;");

            ChatMessage chatMessage = null;

            foreach(var mes in messages) {
                String username = GetUsername(mes.GetValue<Guid>("uid").ToString()); // ClientManager.FindByID() funktioniert nicht da der User auch offline sein kann
                String message = mes.GetValue<String>("message");
                DateTime time = mes.GetValue<DateTime>("time");
                String filePath = mes.GetValue<String>("file");
                bool wasEdited = mes.GetValue<bool>("edited");
                String messageId = mes.GetValue<Guid>("id").ToString();

                chatMessage = new ChatMessage(username, message, filePath, messageId, time, "", wasEdited);
            }

            return chatMessage;
        }

        public static String CreateChat(String name, String desc = "") {
            Guid uuid = Guid.NewGuid();

            session.Execute("INSERT INTO chat (id, name, description, icon) VALUES (" +
                    uuid + ", " +
                    name + ", " +
                    desc + ", " +
                    "" + // Chat Icon später hinzufügen
                ");");


            return uuid.ToString();
        }

        public static String GetUID(String email) { 
            var uid = session.Execute("SELECT uid FROM users where email = '" + email + "' ALLOW FILTERING;"); // Nach besseren Weg überlegen

            foreach(var id in uid)
                return id.GetValue<Guid>("uid").ToString();

            return "";
        }

        public static String GetUsername(String uid) {
            var uids = session.Execute("SELECT username FROM users where uid = " + uid + ";"); 

            foreach(var id in uids)
                return id.GetValue<String>("username");

            return "";
        }

        public static DateTime Test() {
            var result = session.Execute("SELECT time FROM messages;");

            foreach(var r in result)
                return r.GetValue<DateTime>("time");
        
            return DateTime.Now;
        }

    }
}
