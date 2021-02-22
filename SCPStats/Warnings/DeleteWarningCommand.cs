using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using MEC;
using RemoteAdmin;

namespace SCPStats.Websocket.Warnings
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DeleteWarningCommand : ICommand
    {
        public string Command { get; } = "deletewarning";
        public string[] Aliases { get; } = new string[] {"deletewarnings", "delwarning", "delwarnings", "delwarn", "deletewarns", "deletewarn", "delwarns"};
        public string Description { get; } = "Delete a warning.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player pl = null;
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.deletewarning"))
                {
                    response = "You do not have permission to run this command!";
                    return true;
                }

                pl = p;
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = "Usage: deletewarning <id>";
                return true;
            }
            
            var msgId = WebsocketRequests.Random.Next(1000, 9999).ToString();
            foreach (var keys in WebsocketRequests.MessageIDs.Where(pair => pair.Value == pl)) WebsocketRequests.MessageIDs.Remove(keys.Key);
            WebsocketRequests.MessageIDs[msgId] = pl;
            
            WebsocketHandler.SendRequest(RequestType.DeleteWarnings, msgId+arguments.Array[1]);

            response = "Deleting warning...";
            return true;
        }
    }
}