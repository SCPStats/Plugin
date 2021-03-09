using System.Collections.Generic;
using System.Linq;

namespace SCPStats.Websocket.Data
{
    public class RoundStatsData
    {
        public Dictionary<string, Stats> PlayerStats { get; set; }
        
        public string[] KillsByScore { get; }
        public string[] PlayerKillsByScore { get; }
        public string[] ScpKillsByScore { get; }
        public string[] KillsByOrder { get; }
        public string[] PlayerKillsByOrder { get; }
        public string[] ScpKillsByOrder { get; }
        
        public string[] DeathsByScore { get; }
        public string[] DeathsByOrder { get; }
        
        public string[] EscapesByScore { get; }
        public string[] EscapesByOrder { get; }
        public string[] FastestEscapeByScore { get; }
        
        public string[] SodasByScore { get; }
        public string[] SodasByOrder { get; }
        public string[] MedkitsByScore { get; }
        public string[] MedkitsByOrder { get; }
        public string[] BallsByScore { get; }
        public string[] BallsByOrder { get; }
        public string[] AdrenalineByScore { get; }
        public string[] AdrenalineByOrder { get; }
        
        public string[] XpByScore { get; }

        public RoundStatsData(string data)
        {
            var parts = data.Split(',');

            PlayerStats = new Dictionary<string, Stats>();
            
            KillsByScore = CreateScoreArray(parts[0], "Kills");
            PlayerKillsByScore = CreateScoreArray(parts[1], "PlayerKills");
            ScpKillsByScore = CreateScoreArray(parts[2], "ScpKills");
            KillsByOrder = parts[3].Split('|');
            PlayerKillsByOrder = parts[4].Split('|');
            ScpKillsByOrder = parts[5].Split('|');
            
            DeathsByScore = CreateScoreArray(parts[6], "Deaths");
            DeathsByOrder = parts[7].Split('|');
            
            EscapesByScore = CreateScoreArray(parts[8], "Escapes");
            FastestEscapeByScore = CreateScoreArray(parts[9], "FastestEscape");
            EscapesByOrder = parts[10].Split('|');

            SodasByScore = CreateScoreArray(parts[11], "Sodas");
            MedkitsByScore = CreateScoreArray(parts[12], "Medkits");
            BallsByScore = CreateScoreArray(parts[13], "Balls");
            AdrenalineByScore = CreateScoreArray(parts[14], "Adrenaline");
            SodasByOrder = parts[15].Split('|');
            MedkitsByOrder = parts[16].Split('|');
            BallsByOrder = parts[17].Split('|');
            AdrenalineByOrder = parts[18].Split('|');
            
            XpByScore = CreateScoreArray(parts[19], "Xp");
        }

        private string[] CreateScoreArray(string data, string propName)
        {
            return string.IsNullOrEmpty(data) ? new string[] {} : data.Split('|').Select(scoreData => ParseScore(scoreData, propName)).ToArray();
        }

        private string ParseScore(string data, string propName)
        {
            var parts = data.Split(';');

            var user = parts[0];
            var score = int.Parse(parts[1]);
            
            if(!PlayerStats.ContainsKey(user)) PlayerStats[user] = new Stats();
            typeof(Stats).GetField(propName)?.SetValue(PlayerStats[user], score);

            return user;
        }
    }
}