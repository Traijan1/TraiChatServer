using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TraiChatServer {
    class Client {
        String name;
        String id;
        Socket socket;
        Chat chat;

        public String Name { get { return name; } } 
        public String ID { get { return id; } } 
        public Socket Socket { get { return socket; } }

        public Chat Chat { get { return chat; } set { chat = value; } }

        public Client(String email, Socket socket) { // Datenbankanfragen im Konstruktor gut?
            id = Database.GetUID(email);
            name = Database.GetUsername(id);
            this.socket = socket;
        }

        public void ChangeUsername(String newName) {
            // Update Database
            // Send to all Clients an update
            // Confirm update (send to Client)
        }

        public Task Send(SocketMessage message) {
            return Task.Run(() => socket.Send(message.ToJSONBytes()));
        }

        public Task SendMessage(Client source, String message, String file, String messageId, String reply) {
            return Task.Run(() => {
                var chat = new ChatMessage(source.Name, message, file, messageId, DateTime.Now, reply, false);
                var m = new SocketMessage(MessageType.Message);
                m.AddHeaderData("message", chat.ToJSON());

                socket.Send(m.ToJSONBytes());
            });
        }
    }
}
