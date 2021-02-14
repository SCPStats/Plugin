using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;

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
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warn"))
                {
                    response = "You do not have permission to run this command!";
                    return true;
                }
            }
            
            if (arguments.Array == null || arguments.Array.Length < 1)
            {
                response = "Usage: warn <id> [reason]";
                return true;
            }

            var message = "Unspecified";

            if (arguments.Count > 1)
            {
                var messageList = arguments.Array.ToList();
                messageList.RemoveAt(0);

                message = string.Join(" ", messageList);
            }

            var player = Player.Get(arguments.Array[0]);

            if (player?.UserId == null || player.IsHost || !player.IsVerified || player.IPAddress == "127.0.0.WAN" || player.IPAddress == "127.0.0.1")
            {
                response = "The specified player was not found!";
                return true;
            }
            
            StatHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"0\",\"playerId\":\""+Helper.HandleId(player)+"\",\"message\":\""+message.Replace("\"", "\\\"")+"\"}");

            response = "Added warning.";
            return true;
        }
    }
}