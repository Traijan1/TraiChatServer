using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TraiChatServer {

    [Serializable]
    public enum MessageType {
        Verify, // Server sends Verify-Message to Client and Client answers with Verify-Message
        Disconnect,
    }

    [Serializable]
    public class SocketMessage {
        public Dictionary<String, String> Header { get; private set; }
        public MessageType MessageType { get; private set; }

        public SocketMessage(MessageType messageType) {
            Header = new Dictionary<String, String>();
            MessageType = messageType;
        }

        public void AddHeaderData(String key, String data) {
            Header.Add(key, data);
        }

        public byte[] ToJSONBytes() {
            String json = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(json);
        }

        public static SocketMessage FromJSONBytes(byte[] json) {
            String s = Encoding.UTF8.GetString(json);
            return JsonConvert.DeserializeObject<SocketMessage>(s);
        }
    }
}
