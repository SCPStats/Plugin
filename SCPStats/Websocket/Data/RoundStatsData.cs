// -----------------------------------------------------------------------
// <copyright file="RoundStatsData.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PluginAPI.Core;

namespace SCPStats.Websocket.Data
{
    public class RoundStatsData
    {
        public Dictionary<Player, Stats> PlayerStats { get; set; }
        
        public Player[] KillsByScore { get; }
        public Player[] PlayerKillsByScore { get; }
        public Player[] ScpKillsByScore { get; }
        public Player[] KillsByOrder { get; }
        public Player[] PlayerKillsByOrder { get; }
        public Player[] ScpKillsByOrder { get; }
        
        public Player[] DeathsByScore { get; }
        public Player[] DeathsByOrder { get; }
        
        public Player[] EscapesByScore { get; }
        public Player[] EscapesByOrder { get; }
        public Player[] FastestEscapeByScore { get; }
        
        public Player[] SodasByScore { get; }
        public Player[] SodasByOrder { get; }
        public Player[] MedkitsByScore { get; }
        public Player[] MedkitsByOrder { get; }
        public Player[] BallsByScore { get; }
        public Player[] BallsByOrder { get; }
        public Player[] AdrenalineByScore { get; }
        public Player[] AdrenalineByOrder { get; }
        
        public Player[] XpByScore { get; }

        public RoundStatsData(string data)
        {
            var parts = data.Split(',');

            PlayerStats = new Dictionary<Player, Stats>();

            var players = Player.GetPlayers().Where(pl => !Helper.IsPlayerNPC(pl) && !pl.IsServer && pl.IsReady).ToDictionary(Helper.HandleId, pl => pl);
            
            KillsByScore = CreateScoreArray(parts[0], "Kills", players);
            PlayerKillsByScore = CreateScoreArray(parts[1], "PlayerKills", players);
            ScpKillsByScore = CreateScoreArray(parts[2], "ScpKills", players);
            KillsByOrder = IDsToPlayers(parts[3].Split('|'), players);
            PlayerKillsByOrder = IDsToPlayers(parts[4].Split('|'), players);
            ScpKillsByOrder = IDsToPlayers(parts[5].Split('|'), players);
            
            DeathsByScore = CreateScoreArray(parts[6], "Deaths", players);
            DeathsByOrder = IDsToPlayers(parts[7].Split('|'), players);
            
            EscapesByScore = CreateScoreArray(parts[8], "Escapes", players);
            FastestEscapeByScore = CreateScoreArray(parts[9], "FastestEscape", players);
            EscapesByOrder = IDsToPlayers(parts[10].Split('|'), players);

            SodasByScore = CreateScoreArray(parts[11], "Sodas", players);
            MedkitsByScore = CreateScoreArray(parts[12], "Medkits", players);
            BallsByScore = CreateScoreArray(parts[13], "Balls", players);
            AdrenalineByScore = CreateScoreArray(parts[14], "Adrenaline", players);
            SodasByOrder = IDsToPlayers(parts[15].Split('|'), players);
            MedkitsByOrder = IDsToPlayers(parts[16].Split('|'), players);
            BallsByOrder = IDsToPlayers(parts[17].Split('|'), players);
            AdrenalineByOrder = IDsToPlayers(parts[18].Split('|'), players);
            
            XpByScore = CreateScoreArray(parts[19], "Xp", players);
        }

        private Player[] CreateScoreArray(string data, string propName, Dictionary<string, Player> playersDict)
        {
            return string.IsNullOrEmpty(data) ? new Player[] {} : data.Split('|').Select(scoreData => ParseScore(scoreData, propName, playersDict)).Where(x => x != null).ToArray();
        }

        private Player[] IDsToPlayers(IEnumerable<string> ids, Dictionary<string, Player> playersDict)
        {
            var players = new List<Player>();

            foreach (var id in ids)
            {
                if (playersDict.TryGetValue(id, out var player))
                {
                    players.Add(player);
                }
            }

            return players.ToArray();
        }

        private Player ParseScore(string data, string propName, Dictionary<string, Player> playersDict)
        {
            var parts = data.Split(';');

            var user = parts[0];
            if(!playersDict.TryGetValue(user, out var player)) return null;
            
            var score = int.Parse(parts[1], NumberStyles.Integer, Helper.UsCulture);
            
            if(!PlayerStats.ContainsKey(player)) PlayerStats[player] = new Stats();
            typeof(Stats).GetField(propName)?.SetValue(PlayerStats[player], score);

            return player;
        }
    }
}