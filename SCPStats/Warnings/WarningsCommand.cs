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
        public string Command { get; } = "warnings";
        public string[] Aliases { get; } = new string[] {"warning", "warns", "getwarns", "getwarnings"};
        public string Description { get; } = "View warnings on a specific player.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player pl = null;
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warnings"))
                {
                    response = "You do not have permission to run this command!";
                    return true;
                }

                pl = p;
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = "Usage: warnings <id>";
                return true;
            }

            var arg = arguments.Array[1].Trim().ToLower();

            var player = Player.Get(arg);

            if (player == null && int.TryParse(arg, out var id))
            {
                player = Player.Get(id);
            }

            var userId = Helper.HandleId(arg);

            if (player?.RawUserId != null)
            {
                userId = Helper.HandleId(player);
            }

            var msgId = WebsocketRequests.Random.Next(1000, 9999).ToString();
            foreach (var keys in WebsocketRequests.MessageIDs.Where(pair => pair.Value == pl)) WebsocketRequests.MessageIDs.Remove(keys.Key);
            WebsocketRequests.MessageIDs[msgId] = pl;
            
            StatHandler.SendRequest(RequestType.GetWarnings, msgId+userId);

            response = "Requesting warnings...";
            return true;
        }
    }
}