using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using MEC;
using RemoteAdmin;

namespace SCPStats.Warnings
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class WarningsCommand : ICommand
    {
        internal static Player player = null;
        
        public string Command { get; } = "warnings";
        public string[] Aliases { get; } = new string[] {"warning", "warns", "getwarns", "getwarnings"};
        public string Description { get; } = "View warnings on a specific player.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warnings"))
                {
                    response = "You do not have permission to run this command!";
                    return true;
                }

                WarningsCommand.player = p;
            }
            else
            {
                WarningsCommand.player = null;
            }
            
            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = "Usage: warnings <id>";
                return true;
            }

            var player = Player.Get(arguments.Array[1]);

            if (player == null && int.TryParse(arguments.Array[1], out var id))
            {
                player = Player.Get(id);
            }

            if (player?.UserId == null || player.IsHost || !player.IsVerified || player.IPAddress == "127.0.0.WAN" || player.IPAddress == "127.0.0.1")
            {
                response = "The specified player was not found!";
                return true;
            }
            
            StatHandler.SendRequest(RequestType.GetWarnings, Helper.HandleId(player));

            response = "Requesting warnings...";
            return true;
        }
    }
}