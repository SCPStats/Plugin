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
    public class DeleteWarningCommand : ICommand
    {
        internal static Player player = null;
        
        public string Command { get; } = "deletewarning";
        public string[] Aliases { get; } = new string[] {"deletewarnings", "delwarning", "delwarnings"};
        public string Description { get; } = "Delete a warning.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.deletewarning"))
                {
                    response = "You do not have permission to run this command!";
                    return true;
                }

                player = p;
            }
            else
            {
                player = null;
            }
            
            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = "Usage: deletewarning <id>";
                return true;
            }

            Timing.RunCoroutine(WebsocketRequests.DequeueRequests(.5f));
            StatHandler.SendRequest(RequestType.DeleteWarnings, arguments.Array[1]);

            response = "Deleting warning...";
            return true;
        }
    }
}