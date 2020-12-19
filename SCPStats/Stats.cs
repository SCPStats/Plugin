/*using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Synapse.Api;

namespace SCPStats
{
    public static class RoundStats
    {
        internal static Dictionary<string, int> Kills = new Dictionary<string, int>();
        internal static Dictionary<string, int> Deaths = new Dictionary<string, int>();
        internal static Dictionary<string, int> Escapes = new Dictionary<string, int>();

        internal static void Reset()
        {
            Kills.Clear();
            Deaths.Clear();
            Escapes.Clear();
        }

        private static Player GetPlayer(string id)
        {
            return Player.UserIdsCache.TryGetValue(id, out var player) ? player : null;
        }

        public static int GetRoundKills(this Player p) => Kills.TryGetValue(p.UserId, out var kills) ? kills : 0;
        public static int GetRoundDeaths(this Player p) => Deaths.TryGetValue(p.UserId, out var deaths) ? deaths : 0;

        public static float GetKD(this Player p)
        {
            var kills = p.GetRoundKills();
            var deaths = p.GetRoundDeaths();

            return kills == 0 || deaths == 0 ? 0 : (float) kills / deaths;
        }

        public static Player GetMostKills()
        {
            if (Kills.Count < 1) return null;
            var id = Kills.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

            return GetPlayer(id);
        }
        
        public static Player GetLeastKills()
        {
            if (Kills.Count < 1) return null;
            var id = Kills.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

            return GetPlayer(id);
        }
        
        public static Player GetMostDeaths()
        {
            if (Deaths.Count < 1) return null;
            var id = Deaths.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

            return GetPlayer(id);
        }
        
        public static Player GetLeastDeaths()
        {
            if (Deaths.Count < 1) return null;
            var id = Deaths.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

            return GetPlayer(id);
        }

        public static Player GetBestKD()
        {
            return Player.List.OrderBy(p => p.GetKD()).FirstOrDefault();
        }
        
        public static Player GetWorstKD()
        {
            return Player.List.OrderBy(p => p.GetKD()).LastOrDefault();
        }

        public static Player GetFirstEscape()
        {
            return Escapes.Count < 1 ? null : GetPlayer(Escapes.Keys.First());
        }

        public static Player GetFirstKill()
        {
            return Kills.Count < 1 ? null : GetPlayer(Kills.Keys.First());
        }
        
        public static Player GetFirstDeath()
        {
            return Deaths.Count < 1 ? null : GetPlayer(Deaths.Keys.First());
        }

        public static List<Player> GetEscapes()
        {
            return Escapes.Keys.Select(GetPlayer).Where(pl => pl != null).ToList();
        }
    }
}*/