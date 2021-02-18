using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Exiled.API.Features;
using Exiled.Loader;

namespace SCPStats
{
    internal static class Helper
    {
        internal static Dictionary<string, int> Rankings = new Dictionary<string, int>()
        {
            {"kills", 0},
            {"kill", 0},
            {"deaths", 1},
            {"death", 1},
            {"dies", 1},
            {"die", 1},
            {"rounds", 2},
            {"round", 2},
            {"roundsplayed", 2},
            {"playtime", 3},
            {"gametime", 3},
            {"hours", 3},
            {"sodas", 4},
            {"soda", 4},
            {"cokes", 4},
            {"coke", 4},
            {"colas", 4},
            {"cola", 4},
            {"scp207s", 4},
            {"scp207", 4},
            {"scp-207s", 4},
            {"scp-207", 4},
            {"medkits", 5},
            {"medkit", 5},
            {"health", 5},
            {"healthpack", 5},
            {"healthpacks", 5},
            {"balls", 6},
            {"ball", 6},
            {"scp018s", 6},
            {"scp018", 6},
            {"scp18s", 6},
            {"scp18", 6},
            {"scp-018s", 6},
            {"scp-018", 6},
            {"scp-18s", 6},
            {"scp-18", 6},
            {"adrenaline", 7},
            {"adrenalines", 7},
            {"escape", 8},
            {"escapes", 8},
            {"leaderboard", 9},
            {"leaderboards", 9},
            {"xp", 9},
            {"xps", 9},
            {"fastestescape", 10},
            {"fastescape", 10},
            {"quickestescape", 10},
            {"quickescape", 10}
        };
        
        internal static bool IsPlayerValid(Player p, bool dnt = true, bool role = true)
        {
            var playerIsSh = ((List<Player>) Loader.Plugins.FirstOrDefault(pl => pl.Name == "SerpentsHand")?.Assembly.GetType("SerpentsHand.API.SerpentsHand")?.GetMethod("GetSHPlayers")?.Invoke(null, null))?.Any(pl => pl.Id == p.Id) ?? false;

            if (dnt && p.DoNotTrack) return false;
            if (role && (p.Role == RoleType.None || p.Role == RoleType.Spectator)) return false;
            return !(p.Role == RoleType.Tutorial && !playerIsSh);
        }
        
        internal static string HmacSha256Digest(string secret, string message)
        {
            var encoding = new UTF8Encoding();
            
            return BitConverter.ToString(new HMACSHA256(encoding.GetBytes(secret)).ComputeHash(encoding.GetBytes(message))).Replace("-", "").ToLower();
        }
        
        internal static string HandleId(string id)
        {
            return id.Split('@')[0];
        }

        internal static string HandleId(Player player)
        {
            return HandleId(player.UserId);
        }
    }
}