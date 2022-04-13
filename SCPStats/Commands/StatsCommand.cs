// -----------------------------------------------------------------------
// <copyright file="StatsCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using Exiled.API.Features;
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
            // We should also send it if the stats/ranks are empty.
            Tuple<CentralAuthPreauthFlags?, UserInfoData> dataTuple;
            if (!EventHandler.UserInfo.TryGetValue(Helper.HandleId(p), out dataTuple) || dataTuple.Item2.Stats.Length < 1 || dataTuple.Item2.Ranks.Length < 1)
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
            
            response += (SCPStats.Singleton?.Translation?.StatsKD ?? "K/D") + " - " + kd + "\n";
            response += (SCPStats.Singleton?.Translation?.StatsKills ?? "Kills") + " - " + kills + " - " + data.Ranks[Helper.Rankings["kills"]] + "\n";
            response += (SCPStats.Singleton?.Translation?.StatsPlaytime ?? "Playtime") + " - " + playtime + " - " + data.Ranks[Helper.Rankings["playtime"]] + "\n";
            response += (SCPStats.Singleton?.Translation?.StatsRounds ?? "Rounds Played") + " - " + rounds + " - " + data.Ranks[Helper.Rankings["rounds"]] + "\n";
            response += (SCPStats.Singleton?.Translation?.StatsSodas ?? "Sodas Consumed") + " - " + sodas + " - " + data.Ranks[Helper.Rankings["sodas"]] + "\n";
            response += (SCPStats.Singleton?.Translation?.StatsEscapes ?? "Escapes") + " - " + escapes + " - " + data.Ranks[Helper.Rankings["escapes"]] + "\n";
            response += (SCPStats.Singleton?.Translation?.StatsFastestEscape ?? "Fastest Escape") + " - " + fastestEscape + " - " + data.Ranks[Helper.Rankings["fastestescape"]];

            return true;
        }
    }
}