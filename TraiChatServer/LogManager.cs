using System;
using System.Collections.Generic;
using System.Text;

namespace TraiChatServer {
    static class LogManager {
        static String logFile = "log.txt";

        public static void LogConnection(String text) {
            Console.WriteLine("[CONNECTION] " + text);
        }

        public static void LogChatEvent(String text) {
            Console.WriteLine("[CHAT] " + text);
        }
        public static void LogStartup(String text) {
            Console.WriteLine("[STARTUP] " + text);
        }

        public static void LogWarning(String text) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARNING] " + text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogError(String text) {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("[WARNING] " + text);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
