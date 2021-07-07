using System;
using CommandSystem;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SilentWarnCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.SilentWarnCommand ?? "silentwarn";
        public string[] Aliases { get; } = new string[] {"swarn"};
        public string Description => SCPStats.Singleton?.Translation?.SilentWarnDescription ?? "Silently warn a player (without showing a message on their screen).";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            return WarnCommand.ExecuteCustomDisplay(arguments, sender, out response, false);
        }
    }
}