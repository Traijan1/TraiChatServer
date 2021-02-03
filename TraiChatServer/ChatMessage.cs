using System;
using System.Collections.Generic;
using System.Text;
using Cassandra;
using Newtonsoft.Json;

namespace TraiChatServer {
    public class ChatMessage {
        // Später noch TimeStamp
        public String Author { get; private set; }
        public String Message { get; private set; }
        public String FilePath { get; private set; }
        public String MessageID { get; private set; }
        public DateTime Time { get; private set; }
        public String ReplyMessage { get; private set; }
        public bool IsEdited { get; private set; }

        public ChatMessage(String author, String message, String filePath, String messageId, DateTime time, String replyMessage, bool isEdited) {
            Author = author;
            Message = message;
            FilePath = filePath;
            MessageID = messageId;
            Time = time;
            ReplyMessage = replyMessage;
            IsEdited = isEdited;
        }

        public String ToJSON() {
            return JsonConvert.SerializeObject(this);
        }

        public static ChatMessage FromJSON(String json) {
            return JsonConvert.DeserializeObject<ChatMessage>(json);
        }
    }
}
