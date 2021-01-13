using System;
using System.Security.Cryptography;
using System.Text;
using ETAPI.Enums;
using ETAPI.Features;
using PluginFramework.Classes;
using VirtualBrightPlayz.SCP_ET.Player;
using VirtualBrightPlayz.SCP_ET.ServerGroups;

namespace SCPStats
{
    internal static class Helper
    {
        internal static bool IsPlayerValid(Player p, bool dnt = true, bool role = true)
        {
            return (!dnt || true) && !string.IsNullOrEmpty(p.SteamID) && (!role || (p.Role != Role.None && p.Role != Role.Spectator));
        }
        
        internal static string HmacSha256Digest(string secret, string message)
        {
            var encoding = new ASCIIEncoding();
            
            return BitConverter.ToString(new HMACSHA256(encoding.GetBytes(secret)).ComputeHash(encoding.GetBytes(message))).Replace("-", "").ToLower();
        }

        internal static bool CheckPermissions(this Player player, string node)
        {
            return ServerGroups.CheckPermission(player.PlayerController.ConnectionToClient, node);
        }
    }
}