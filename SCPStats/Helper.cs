using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            {"deaths", 1},
            {"rounds", 2},
            {"playtime", 3},
            {"sodas", 4},
            {"medkits", 5},
            {"balls", 6},
            {"adrenaline", 7}
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
            var encoding = new ASCIIEncoding();
            
            return BitConverter.ToString(new HMACSHA256(encoding.GetBytes(secret)).ComputeHash(encoding.GetBytes(message))).Replace("-", "").ToLower();
        }
        
        internal static string HandleId(string id)
        {
            return id.Split('@')[0];
        }

        internal static string HandleId(Player player)
        {
            return HandleId(player.RawUserId);
        }
    }
}