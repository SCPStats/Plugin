// -----------------------------------------------------------------------
// <copyright file="WarningsCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using SCPStats.API;
using SCPStats.API.EventArgs;
using SCPStats.Websocket;
using SCPStats.Websocket.Data;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class WarningsCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.WarningsCommand ?? "warnings";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.WarningsCommandAliases?.ToArray() ?? new string[] {"warning", "warns", "getwarns", "getwarnings"};
        public string Description => SCPStats.Singleton?.Translation?.WarningsDescription ?? "View warnings on a specific player.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player pl = null;
            
            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.warnings"))
                {
                    response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                    return false;
                }

                pl = p;
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = SCPStats.Singleton?.Translation?.WarningsUsage ?? "Usage: warnings <id>";
                return false;
            }

            var arg = arguments.Array[1].Trim().ToLower();

            var player = Player.Get(arg);

            if (player == null && int.TryParse(arg, out var id))
            {
                player = Player.Get(id);
            }

            var userId = Helper.HandleId(arg);

            if (player?.UserId != null)
            {
                userId = Helper.HandleId(player);
            }

            Task.Run(async () =>
            {
                var warnings = await API.API.GetWarnings(userId);
                SendWarningsMessage(pl, warnings);
            });

            response = SCPStats.Singleton?.Translation?.WarningsSuccess ?? "Requesting warnings...";
            return true;
        }

        private static void SendWarningsMessage(Player sender, List<Warning> warningsList)
        {
            warningsList = warningsList.Where(warning => (SCPStats.Singleton?.Translation?.WarningsDisplayedTypes ?? WarningsDisplayedTypes).Contains(warning.Type)).ToList();

            var result = SCPStats.Singleton?.Translation?.Warnings ?? "\nID | Type | Message | Ban Length\n\n";

            var generatingEventArgs = new GeneratingWarningMessageEventArgs(warningsList, result);
            Events.OnGeneratingWarningMessage(generatingEventArgs);

            result += String.Join("\n", generatingEventArgs.Warnings.Select(warning =>
            {
                var message = new List<string>();

                foreach (var section in SCPStats.Singleton?.Translation?.WarningsDisplayedSections ?? WarningsDisplayedSections)
                {
                    switch (section)
                    {
                        case WarningSection.ID:
                            message.Add(warning.ID.ToString());
                            break;
                        case WarningSection.Type:
                            message.Add(warning.Type.GetWarningTypeName());
                            break;
                        case WarningSection.Message:
                            message.Add(warning.Message);
                            break;
                        case WarningSection.Length:
                            if (warning.Type != WarningType.Ban)
                            {
                                message.Add("");
                                break;
                            }

                            message.Add((SCPStats.Singleton?.Translation?.WarningsPrettyPrintSeconds ?? true) ? Helper.SecondsToString(warning.Length) : warning.Length + " " + (SCPStats.Singleton?.Translation?.TimeSeconds ?? "second(s)"));
                            break;
                        case WarningSection.Issuer:
                            message.Add(warning.Issuer);
                            break;
                    }
                }

                return WarningCleanupRegex.Replace(String.Join(SCPStats.Singleton?.Translation?.WarningsSectionSeparator ?? " | ", message), "");
            }));

            var sendingEventArgs = new SendingWarningMessageEventArgs(warningsList, result);
            Events.OnSendingWarningMessage(sendingEventArgs);

            if (sender != null)
            {
                sender.RemoteAdminMessage(sendingEventArgs.Message, true, SCPStats.Singleton?.Translation?.WarningsCommand?.ToUpper() ?? "WARNINGS");
            }
            else
            {
                ServerConsole.AddLog(sendingEventArgs.Message);
            }
        }

        private static List<WarningType> WarningsDisplayedTypes { get; set; } = new List<WarningType>()
        {
            WarningType.Warning,
            WarningType.Note,
            WarningType.Ban,
            WarningType.Kick,
            WarningType.Mute,
            WarningType.IntercomMute
        };

        private static List<WarningSection> WarningsDisplayedSections { get; set; } = new List<WarningSection>()
        {
            WarningSection.ID,
            WarningSection.Type,
            WarningSection.Message,
            WarningSection.Length,
            WarningSection.Issuer
        };

        private static Regex WarningCleanupRegex = new Regex("(?:\\s+\\|)+\\s*$");
    }
}