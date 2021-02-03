using System;
using System.Collections.Generic;
using System.Text;

namespace TraiChatServer {
    class ChatManager {
        static List<Chat> chats = new List<Chat>();

        public static List<Chat> Chats { get { return chats; } }

        public static bool CreateChat(Chat chat) {
            if(chats.Contains(chat))
                return false; // Send to user that chat couldn't be created 

            // Send to all users that chat is created

            return true;
        }
    }
}
