using System;
using System.Collections.Generic;
using System.Text;

namespace TraiChatServer {
    class Chat {
        List<Client> users;

        String id;
        String name;

        public List<Client> Users { get { return users; } }
        public String ID { get { return id; } }
        public String Name { get { return name; } }

        public Chat(String id) {
            this.id = id;
            // name = 
        }

        public void Join(Client user) {

        }

        public void Disconnect(Client user) {

        }
    }
}
