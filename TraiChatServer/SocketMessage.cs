using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace TraiChatServer {
    [Serializable]
    public enum MessageType {
        Verify, // Server sends Verify-Message to Client and Client answers with Verify-Message

    }

    [Serializable]
    public class SocketMessage {
        public Dictionary<String, String> Header { get; private set; }
        public MessageType MessageType { get; private set; }

        public SocketMessage(MessageType type) {
            Header = new Dictionary<String, String>();
            MessageType = type;
        }

        public void AddHeaderData(String key, String data) {
            Header.Add(key, data);
        }

        public byte[] ToJSONBytes() {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

        public static SocketMessage FromJSONBytes(byte[] json) {
            return JsonConvert.DeserializeObject<SocketMessage>(Encoding.UTF8.GetString(json));
        }
    }
}
