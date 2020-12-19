using System;

namespace SCPStats
{
    internal static class Log
    {
        internal static void Info(object message)
        {
            ServerConsole.AddLog($"[INFO] [SCPStats] {message}", ConsoleColor.Cyan);
        }
        
        internal static void Warn(object message)
        {
            ServerConsole.AddLog($"[WARN] [SCPStats] {message}", ConsoleColor.Magenta);
        }
        
        internal static void Error(object message)
        {
            ServerConsole.AddLog($"[ERROR] [SCPStats] {message}", ConsoleColor.Red);
        }
    }
}