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
        public String Description { get; private set; }
        public String ChatIcon { get; private set; }
        public bool Primary { get; private set; }

        public Chat(String id) {
            this.id = id;
            // name = 
            // Description = 
        }

        public Chat(String id, String name, String desc, bool primary) {
            users = new List<Client>();

            this.id = id;
            this.name = name;
            Description = desc;
            Primary = primary;
        }

        public void Join(Client user) {
            users.Add(user);

            if(user.Chat != null)
                user.Chat.Disconnect(user);

            user.Chat = this;
        }

        public void Disconnect(Client user) {
            users.Remove(user);
        }
    }
}
