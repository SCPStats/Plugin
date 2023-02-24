// -----------------------------------------------------------------------
// <copyright file="StatsCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using PluginAPI.Core;
using RemoteAdmin;
using SCPStats.Websocket.Data;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class StatsCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.StatsCommand ?? "stats";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.StatsCommandAliases?.ToArray() ?? new string[] {"stat", "serverstat", "serverstats"};
        public string Description => SCPStats.Singleton?.Translation?.StatsDescription ?? "View your stats and ranking for this server.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!(sender is PlayerCommandSender))
            {
                response = SCPStats.Singleton?.Translation?.NotPlayer ?? "This command can only be ran by a player!";
                return true;
            }
            
            var p = Player.Get(((PlayerCommandSender) sender).ReferenceHub);
            
            // If for whatever reason we have no UserInfoData on the player, send the no stats message.
            // We should also send it if the stats are empty.
            Tuple<CentralAuthPreauthFlags?, UserInfoData> dataTuple;
            if (!EventHandler.UserInfo.TryGetValue(Helper.HandleId(p), out dataTuple) || dataTuple?.Item2 == null || dataTuple.Item2.Stats.Length < 1)
            {
                response = SCPStats.Singleton?.Translation?.StatsNoStats ?? "Looks like you don't have any stats. Make sure you've signed up at https://scpstats.com to view your stats.";
                return true;
            }

            var data = dataTuple.Item2;
            
            // Now, we just need to generate the message.
            response = (SCPStats.Singleton?.Translation?.StatsHeader ?? "Here are your stats for this server:\n\nStat - Amount - Rank") + "\n";

            var kills = int.TryParse(data.Stats[Helper.Rankings["kills"]], out var kills1) ? kills1 : 0;
            var deaths = int.TryParse(data.Stats[Helper.Rankings["deaths"]], out var deaths1) ? deaths1 : 0;
            var kd = deaths == 0 ? 0 : (float) kills / deaths;
            var playtime = int.TryParse(data.Stats[Helper.Rankings["playtime"]], out var playtime1) ? Helper.SecondsToHours(playtime1) : Helper.SecondsToHours(0);
            var rounds = data.Stats[Helper.Rankings["rounds"]];
            var sodas = data.Stats[Helper.Rankings["sodas"]];
            var escapes = data.Stats[Helper.Rankings["escapes"]];
            var fastestEscape = int.TryParse(data.Stats[Helper.Rankings["fastestescape"]], out var fastestEscape1) ? Helper.SecondsToString(fastestEscape1) : Helper.SecondsToString(0);

            bool hasRanks = data.Ranks.Length > 0;
            
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsKD ?? "K/D", kd.ToString());
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsKills ?? "Kills", kills.ToString(), getRank(hasRanks, data, "kills"));
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsDeaths ?? "Deaths", deaths.ToString(), getRank(hasRanks, data, "deaths"));
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsPlaytime ?? "Playtime", playtime, getRank(hasRanks, data, "playtime"));
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsRounds ?? "Rounds Played", rounds, getRank(hasRanks, data, "rounds"));
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsSodas ?? "Sodas Consumed", sodas, getRank(hasRanks, data, "sodas"));
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsEscapes ?? "Escapes", escapes, getRank(hasRanks, data, "escapes"));
            response += generateMessage(SCPStats.Singleton?.Translation?.StatsFastestEscape ?? "Fastest Escape", fastestEscape, getRank(hasRanks, data, "fastestescape"), false);

            return true;
        }

        private static String generateMessage(String key, String amount, String rank = "", bool newLine = true)
        {
            return key + " - " + amount + (rank == "" ? "" : " - " + rank) + (newLine ? "\n" : "");
        }

        private static String getRank(bool hasRanks, UserInfoData data, String key)
        {
            if (!hasRanks) return "";

            String value = data.Ranks[Helper.Rankings[key]];

            int valueInt;
            if (!int.TryParse(value, out valueInt))
            {
                return "";
            }

            // We need to add one because it's zero-based.
            return (valueInt + 1).ToString();
        }
    }
}