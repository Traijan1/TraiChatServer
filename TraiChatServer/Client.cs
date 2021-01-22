using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace TraiChatServer {
    class Client {
        String name;
        String id;
        Socket socket;
        Chat chat;

        public String Name { get { return name; } } // Später wird der Name über die ID aus der Datenbank übermittelt
        public String ID { get { return id; } } 
        public Socket Socket { get { return socket; } }
        public Chat Chat { get { return chat; } }

        public Client(String name, String id, Socket socket) {
            this.name = name;
            this.id = id;
            this.socket = socket;
            chat = null; // Oder einen Anfangschat/Willkommenschat
        }

        public void ChangeUsername(String newName) {
            // Update Database
            // Send to all Clients an update
            // Confirm update (send to Client)
        }
    }
}
