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
    public class OWarnCommand : ICommand
    {
        public string Command { get; } = "owarn";
        public string[] Aliases { get; } = new string[] {"offlinewarn"};
        public string Description { get; } = "Warn an offline player.";
        
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
                response = "Usage: owarn <id> [reason]";
                return true;
            }

            var message = "";

            if (arguments.Array.Length > 2)
            {
                var messageList = arguments.Array.ToList();
                messageList.RemoveAt(0);
                messageList.RemoveAt(0);

                message = string.Join(" ", messageList);
            }

            var arg = arguments.Array[1].Trim().ToLower();

            if (!arg.Contains("@"))
            {
                response = "Please enter a valid user id (for example, ID@steam)!";
                return true;
            }

            var userId = Helper.HandleId(arg);

            if (userId.Length > 18)
            {
                response = "User IDs have a maximum length of 18 characters. The one you have input is larger than that!";
                return true;
            }

            if (!arg.EndsWith("@northwood") && !long.TryParse(userId, out _))
            {
                response = "User IDs cannot contain non-numbers!";
                return true;
            }
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"0\",\"playerId\":\""+userId.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"message\":\""+message.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+issuerID+"\",\"issuerName\":\""+issuerName.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"}");

            response = "Added warning.";
            return true;
        }
    }
}