using System;
using CommandSystem;
using Exiled.Permissions.Extensions;
using RemoteAdmin;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class PauseStatsCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.PauseStatsCommand ?? "pausestats";
        public string[] Aliases { get; } = new string[] {"pausestat", "pausescpstats", "pausescpstat", "pauseround"};
        public string Description => SCPStats.Singleton?.Translation?.PauseStatsDescription ?? "Temporarily pause stat collection for the round. Useful for events.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender p && !p.CheckPermission("scpstats.pause"))
            {
                response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                return true;
            }

            EventHandler.PauseRound = true;
            response = SCPStats.Singleton?.Translation?.PauseStatsSuccess ?? "Successfully paused stat collection for the round.";
            return true;
        }
    }
}