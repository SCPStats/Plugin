// -----------------------------------------------------------------------
// <copyright file="OWarnCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

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
        public string Command => SCPStats.Singleton?.Translation?.OWarnCommand ?? "owarn";
        public string[] Aliases { get; } = new string[] {"offlinewarn"};
        public string Description => SCPStats.Singleton?.Translation?.OWarnDescription ?? "Warn an offline player.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var issuerID = "";
            var issuerName = "";
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warn"))
                {
                    response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                    return true;
                }
                
                issuerID = Helper.HandleId(p);
                issuerName = p.Nickname;
            }
            
            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = SCPStats.Singleton?.Translation?.OWarnUsage ?? "Usage: owarn <id> [reason]";
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
                response = SCPStats.Singleton?.Translation?.OWarnInvalidID ?? "Please enter a valid user id (for example, ID@steam)!";
                return true;
            }

            var userId = Helper.HandleId(arg);

            if (userId.Length > 18)
            {
                response = SCPStats.Singleton?.Translation?.OWarnIDTooLong ?? "User IDs have a maximum length of 18 characters. The one you have input is larger than that!";
                return true;
            }

            if (!arg.EndsWith("@northwood") && !long.TryParse(userId, out _))
            {
                response = SCPStats.Singleton?.Translation?.OWarnIDNotNumeric ?? "User IDs cannot contain non-numbers!";
                return true;
            }
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"0\",\"playerId\":\""+userId.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"message\":\""+message.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"issuer\":\""+issuerID+"\",\"issuerName\":\""+issuerName.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"}");

            response = SCPStats.Singleton?.Translation?.OWarnSuccess ?? "Added warning.";
            return true;
        }
    }
}