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

                            message.Add((SCPStats.Singleton?.Translation?.WarningsPrettyPrintSeconds ?? true) ? SecondsToString(warning.Length) : warning.Length + " " + (SCPStats.Singleton?.Translation?.TimeSeconds ?? "second(s)"));
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

        private static int Minutes = 60;
        private static int Hours = Minutes * 60;
        private static int Days = Hours * 24;
        private static int Weeks = Days * 7;
        private static int Months = Days * 30;
        private static int Years = Days * 365;

        private static string SecondsToString(int seconds)
        {
            var years = 0;
            var months = 0;
            var weeks = 0;
            var days = 0;
            var hours = 0;
            var minutes = 0;

            if (seconds / Years >= 1)
            {
                years = seconds / Years;
                seconds -= years * Years;
            }

            if (seconds / Months >= 1)
            {
                months = seconds / Months;
                seconds -= months * Months;
            }

            if (seconds / Weeks >= 1)
            {
                weeks = seconds / Weeks;
                seconds -= weeks * Months;
            }

            if (seconds / Days >= 1)
            {
                days = seconds / Days;
                seconds -= days * Days;
            }

            if (seconds / Hours >= 1)
            {
                hours = seconds / Hours;
                seconds -= hours * Hours;
            }

            if (seconds / Minutes >= 1)
            {
                minutes = seconds / Minutes;
                seconds -= minutes * Minutes;
            }

            return String.Join(", ", new Tuple<int, string>[]
            {
                new Tuple<int, string>(years, SCPStats.Singleton?.Translation?.TimeYears ?? "year(s)"),
                new Tuple<int, string>(weeks, SCPStats.Singleton?.Translation?.TimeWeeks ?? "week(s)"),
                new Tuple<int, string>(months, SCPStats.Singleton?.Translation?.TimeMonths ?? "month(s)"),
                new Tuple<int, string>(days, SCPStats.Singleton?.Translation?.TimeDays ?? "day(s)"),
                new Tuple<int, string>(hours, SCPStats.Singleton?.Translation?.TimeHours ?? "hour(s)"),
                new Tuple<int, string>(minutes, SCPStats.Singleton?.Translation?.TimeMinutes ?? "minute(s)"),
                new Tuple<int, string>(seconds, SCPStats.Singleton?.Translation?.TimeSeconds ?? "second(s)")
            }.Where(item => item.Item1 > 0).Select(item => item.Item1 + " " + item.Item2));
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