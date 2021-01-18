using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Synapse.Api;
using SynapseInjector;

namespace SCPStats
{
    internal static class Helper
    {
        internal static bool IsPlayerValid(Player p, bool dnt = true, bool role = true)
        {
            if (p == null) return false;
            if (dnt && p.DoNotTrack) return false;
            if (role && (p.RoleType == RoleType.None || p.RoleType == RoleType.Spectator)) return false;
            return p.RoleType != RoleType.Tutorial;
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
            return HandleId(player.RawUserId());
        }

        internal static string RawUserId(this Player p)
        {
            return p.UserId.Substring(0, p.UserId.LastIndexOf('@'));
        }
    }
}