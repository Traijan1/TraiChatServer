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

        /// <summary>
        /// Sendet eine SocketMessage an den Client
        /// </summary>
        /// <param name="message">Die Socket-Message</param>
        /// <returns>Den asynchronen Task</returns>
        public Task Send(SocketMessage message) {
            return Task.Run(() => socket.Send(message.ToJSONBytes()));
        }

        /// <summary>
        /// Sendet eine Chatnachricht an den Klient
        /// </summary>
        /// <param name="source">Der Klient der die Nachricht geschickt hat</param>
        /// <param name="message">Die Nachricht</param>
        /// <param name="file">Filepath</param>
        /// <param name="messageId">Die ID der Nachricht</param>
        /// <param name="reply">Die Reply Nachricht</param>
        /// <returns></returns>
        public Task SendMessage(Client source, String message, String file, String messageId, String reply) {
            return Task.Run(async () => {
                var chat = new ChatMessage(source.Name, message, file, messageId, DateTime.Now, reply, false);
                var m = new SocketMessage(MessageType.Message);
                m.AddHeaderData("message", chat.ToJSON());

                await Send(m);
            });
        }
    }
}
