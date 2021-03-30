using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using SCPStats.Websocket;

namespace SCPStats.Warnings
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class WarningsCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.WarningsCommand ?? "warnings";
        public string[] Aliases { get; } = new string[] {"warning", "warns", "getwarns", "getwarnings"};
        public string Description => SCPStats.Singleton?.Translation?.WarningsDescription ?? "View warnings on a specific player.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player pl = null;
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warnings"))
                {
                    response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                    return true;
                }

                pl = p;
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = SCPStats.Singleton?.Translation?.WarningsUsage ?? "Usage: warnings <id>";
                return true;
            }

            var arg = arguments.Array[1].Trim().ToLower();

            var player = Player.Get(arg);

            if (player == null && int.TryParse(arg, out var id))
            {
                player = Player.Get(id);
            }

            var userId = Helper.HandleId(arg);

            if (player?.UserId != null)
            {
                userId = Helper.HandleId(player);
            }

            var msgId = WebsocketRequests.Random.Next(1000, 9999).ToString();
            foreach (var keys in WebsocketRequests.MessageIDs.Where(pair => pair.Value == pl).ToList()) WebsocketRequests.MessageIDs.Remove(keys.Key);
            WebsocketRequests.MessageIDs[msgId] = pl;
            
            WebsocketHandler.SendRequest(RequestType.GetWarnings, msgId+userId);

            response = SCPStats.Singleton?.Translation?.WarningsSuccess ?? "Requesting warnings...";
            return true;
        }
    }
}