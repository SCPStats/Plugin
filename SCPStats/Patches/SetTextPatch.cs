using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Extensions;
using Exiled.API.Features;
using HarmonyLib;
using Mirror;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.SetText))]
    public class SetTextPatch
    {
        private static Dictionary<string, string> RoleNames { get; set; } = new Dictionary<string, string>()
        {
            {"ExampleXPRole", "{xp} XP | Level {level}"}
        };

        private static Regex _varRegex = new Regex("{(\\w+?)}");

        public static bool Prefix(ServerRoles __instance, string i)
        {
            var p = Player.Get(__instance._hub);
            var key = Server.PermissionsHandler._groups.FirstOrDefault(kvp => kvp.Value == __instance.Group).Key;
            if (p == null || string.IsNullOrEmpty(i) || string.IsNullOrEmpty(key) || !(SCPStats.Singleton?.Config?.RoleNames ?? RoleNames).TryGetValue(key, out var value)) return true;

            var stats = EventHandler.UserInfo.TryGetValue(Helper.HandleId(p), out var info) && info?.Item2 != null ? info.Item2.Stats : Array.Empty<string>();

            var newName = _varRegex.Replace(value, match =>
            {
                var statKey = match.Groups[1].Value;
                if (!Helper.Rankings.TryGetValue(statKey, out var idx)) return match.Value;

                return stats.Length < idx ? "0" : stats[idx];
            });

            if (NetworkServer.active)
            {
                __instance.Network_myText = newName;
            }
            __instance.MyText = newName;

            var namedColor = __instance.NamedColors.FirstOrDefault(row => row.Name == __instance.MyColor);
            if (namedColor != null)
            {
                __instance.CurrentColor = namedColor;
            }

            return false;
        }
    }
}