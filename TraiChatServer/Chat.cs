using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        public Chat(String id, String name, String desc, bool primary) {
            users = new List<Client>();

            this.id = id;
            this.name = name;
            Description = desc;
            Primary = primary;
        }

        /// <summary>
        /// Fügt einen Client in den Chat hinzu
        /// </summary>
        /// <param name="user">Der hinzuzufpgende Client</param>
        public void Join(Client user) {
            users.Add(user);

            if(user.Chat != null)
                user.Chat.Remove(user);

            user.Chat = this;
        }

        /// <summary>
        /// Entfernt einen Client aus dem Chat
        /// </summary>
        /// <param name="user">Der Client der entfernt werden soll</param>
        public void Remove(Client user) {
            users.Remove(user);
        }

        /// <summary>
        /// Sendet an jeden Client im Chat eine SocketMessage
        /// </summary>
        /// <param name="sm">Die zu versendene SocketMessage</param>
        public void Broadcast(SocketMessage sm) {
            foreach(Client c in users)
                c.Send(sm);
        }
    }
}
