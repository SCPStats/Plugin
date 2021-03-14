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
    public class WarnCommand : ICommand
    {
        public string Command { get; } = "warn";
        public string[] Aliases { get; } = new string[] {};
        public string Description { get; } = "Warn a player.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var issuerID = "";
            var issuerName = "";
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warn"))
                {
                    response = "You do not have permission to run this command!";
                    return true;
                }

                issuerID = Helper.HandleId(p);
                issuerName = p.Nickname;
            }
            
            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = "Usage: warn <id> [reason]";
                return true;
            }

            var message = "Unspecified";

            if (arguments.Array.Length > 2)
            {
                var messageList = arguments.Array.ToList();
                messageList.RemoveAt(0);
                messageList.RemoveAt(0);

                message = string.Join(" ", messageList);
            }

            var player = Player.Get(arguments.Array[1]);
            
            if (player == null && int.TryParse(arguments.Array[1], out var id))
            {
                player = Player.Get(id);
            }

            if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player))
            {
                response = "The specified player was not found! Use the owarn command to warn offline players.";
                return true;
            }
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"0\",\"playerId\":\""+Helper.HandleId(player)+"\",\"message\":\""+message.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"playerName\":\""+player.Nickname+"\",\"issuer\":\""+issuerID+"\",\"issuerName\":\""+issuerName+"\"}");

            response = "Added warning.";
            return true;
        }
    }
}