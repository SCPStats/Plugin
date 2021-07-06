﻿// -----------------------------------------------------------------------
// <copyright file="WarnCommand.cs" company="SCPStats.com">
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

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class WarnCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.WarnCommand ?? "warn";
        public string[] Aliases { get; } = Array.Empty<string>();
        public string Description => SCPStats.Singleton?.Translation?.WarnDescription ?? "Warn a player.";
        
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
                    return false;
                }

                issuerID = Helper.HandleId(p);
                issuerName = p.Nickname;
            }
            
            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = SCPStats.Singleton?.Translation?.WarnUsage ?? "Usage: warn <id> [reason]";
                return false;
            }

            var message = "";

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

            var ID = "";

            if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player))
            {
                var arg = arguments.Array[1].Trim().ToLower();

                if (!arg.Contains("@"))
                {
                    response = SCPStats.Singleton?.Translation?.WarnInvalidId ?? "Please enter a valid user id (for example, ID@steam)!";
                    return false;
                }

                ID = Helper.HandleId(arg);

                if (ID.Length > 18)
                {
                    response = SCPStats.Singleton?.Translation?.WarnIdTooLong ?? "User IDs have a maximum length of 18 characters. The one you have input is larger than that!";
                    return false;
                }

                if (!arg.EndsWith("@northwood") && !long.TryParse(ID, out _))
                {
                    response = SCPStats.Singleton?.Translation?.WarnIdNotNumeric ?? "User IDs cannot contain non-numbers!";
                    return false;
                }
            }
            else
            {
                ID = Helper.HandleId(player);
            }
            
            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"0\",\"playerId\":\""+ID.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"message\":\""+message.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"playerName\":\""+player.Nickname+"\",\"issuer\":\""+issuerID+"\",\"issuerName\":\""+issuerName+"\",\"online\":true}");
            Helper.SendWarningMessage(player, message);
            
            response = SCPStats.Singleton?.Translation?.WarnSuccess ?? "Added warning.";
            return true;
        }
    }
}