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
    public class DeleteWarningCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.DeleteWarningCommand ?? "deletewarning";
        public string[] Aliases { get; } = new string[] {"deletewarnings", "delwarning", "delwarnings", "delwarn", "deletewarns", "deletewarn", "delwarns"};
        public string Description => SCPStats.Singleton?.Translation?.DeleteWarningDescription ?? "Delete a warning.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player pl = null;
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.deletewarning"))
                {
                    response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                    return true;
                }

                pl = p;
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = SCPStats.Singleton?.Translation?.DeleteWarningUsage ?? "Usage: deletewarning <id>";
                return true;
            }
            
            var msgId = WebsocketRequests.Random.Next(1000, 9999).ToString();
            foreach (var keys in WebsocketRequests.MessageIDs.Where(pair => pair.Value == pl).ToList()) WebsocketRequests.MessageIDs.Remove(keys.Key);
            WebsocketRequests.MessageIDs[msgId] = pl;
            
            WebsocketHandler.SendRequest(RequestType.DeleteWarnings, msgId+arguments.Array[1]);

            response = SCPStats.Singleton?.Translation?.DeleteWarningSuccess ?? "Deleting warning...";
            return true;
        }
    }
}