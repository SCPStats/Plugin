using System.Collections.Generic;
using System.Linq;

namespace SCPStats.Data
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
            
            KillsByScore = CreateScoreArray(parts[0], "Kills").ToArray();
            PlayerKillsByScore = CreateScoreArray(parts[1], "PlayerKills").ToArray();
            ScpKillsByScore = CreateScoreArray(parts[2], "ScpKills").ToArray();
            KillsByOrder = parts[3].Split('|');
            PlayerKillsByOrder = parts[4].Split('|');
            ScpKillsByOrder = parts[5].Split('|');
            
            DeathsByScore = CreateScoreArray(parts[6], "Deaths").ToArray();
            DeathsByOrder = parts[7].Split('|');
            
            EscapesByScore = CreateScoreArray(parts[8], "Escapes").ToArray();
            FastestEscapeByScore = CreateScoreArray(parts[9], "FastestEscape").ToArray();
            EscapesByOrder = parts[10].Split('|');

            SodasByScore = CreateScoreArray(parts[11], "Sodas").ToArray();
            MedkitsByScore = CreateScoreArray(parts[12], "Medkits").ToArray();
            BallsByScore = CreateScoreArray(parts[13], "Balls").ToArray();
            AdrenalineByScore = CreateScoreArray(parts[14], "Adrenaline").ToArray();
            SodasByOrder = parts[15].Split('|');
            MedkitsByOrder = parts[16].Split('|');
            BallsByOrder = parts[17].Split('|');
            AdrenalineByOrder = parts[18].Split('|');
            
            XpByScore = CreateScoreArray(parts[19], "Xp").ToArray();
        }

        private IEnumerable<string> CreateScoreArray(string data, string propName)
        {
            return data.Split('|').Select(scoreData => ParseScore(scoreData, propName));
        }

        private string ParseScore(string data, string propName)
        {
            var parts = data.Split(';');

            var user = parts[0];
            var score = int.Parse(parts[1]);
            
            if(!PlayerStats.ContainsKey(user)) PlayerStats[user] = new Stats();
            typeof(Stats).GetProperty(propName)?.SetValue(PlayerStats[user], score);

            return user;
        }
    }
}