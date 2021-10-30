// -----------------------------------------------------------------------
// <copyright file="WarnCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using SCPStats.Websocket;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class WarnCommand : ICommand, IUsageProvider
    {
        public string Command => SCPStats.Singleton?.Translation?.WarnCommand ?? "warn";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.WarnCommandAliases?.ToArray() ?? Array.Empty<string>();
        public string Description => SCPStats.Singleton?.Translation?.WarnDescription ?? "Warn a player.";
        public string[] Usage => SCPStats.Singleton?.Translation?.WarnUsagesList ?? new string[] {"id", "reason"};

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            return ExecuteCustomDisplay(arguments, sender, out response, true, SCPStats.Singleton?.Translation?.WarnUsage ?? "Usage: warn <id> [reason]", SCPStats.Singleton?.Translation?.WarnSuccess ?? "Added warning.");
        }

        internal static bool ExecuteCustomDisplay(ArraySegment<string> arguments, ICommandSender sender, out string response, bool displayed, string usage, string success, int type = 0)
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
                response = usage;
                return false;
            }
            
            string[] array;
            var selectedPlayers = Utils.RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array);

            if (selectedPlayers.Count > 0)
            {
                var message = string.Join(" ", array);

                switch (type)
                {
                    case 0:
                        foreach (var selectedPlayer in selectedPlayers)
                        {
                            var player = Player.Get(selectedPlayer);
                            if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player)) continue;

                            API.API.AddWarning(player, message, issuerID, issuerName, !displayed);
                        }
                        break;
                    case 5:
                        foreach (var selectedPlayer in selectedPlayers)
                        {
                            var player = Player.Get(selectedPlayer);
                            if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player)) continue;

                            API.API.AddNote(player, message, issuerID, issuerName);
                        }
                        break;
                }
            }
            else
            {
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

                if (player?.UserId == null || player.IsHost || !player.IsVerified || Helper.IsPlayerNPC(player))
                {
                    var arg = arguments.Array[1].Trim().ToLower();

                    if (!(SCPStats.Singleton?.Config?.DisableIdAuthCheck ?? false) && !arg.Contains("@"))
                    {
                        response = SCPStats.Singleton?.Translation?.WarnInvalidId ?? "Please enter a valid user id (for example, ID@steam)!";
                        return false;
                    }

                    var userId = Helper.HandleId(arg);

                    if (userId.Length > 18)
                    {
                        response = SCPStats.Singleton?.Translation?.WarnIdTooLong ?? "User IDs have a maximum length of 18 characters. The one you have input is larger than that!";
                        return false;
                    }

                    if (!arg.EndsWith("@northwood") && !long.TryParse(userId, out _))
                    {
                        response = SCPStats.Singleton?.Translation?.WarnIdNotNumeric ?? "User IDs cannot contain non-numbers!";
                        return false;
                    }

                    switch (type)
                    {
                        case 0:
                            API.API.AddWarning(userId, "", message, issuerID, issuerName, !displayed);
                            break;
                        case 5:
                            API.API.AddNote(userId, "", message, issuerID, issuerName);
                            break;
                    }
                }
                else
                {
                    switch (type)
                    {
                        case 0:
                            API.API.AddWarning(player, message, issuerID, issuerName, !displayed);
                            break;
                        case 5:
                            API.API.AddNote(player, message, issuerID, issuerName);
                            break;
                    }
                }
            }

            response = success;
            return true;
        }
    }
}