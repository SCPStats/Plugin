// -----------------------------------------------------------------------
// <copyright file="SetTextPatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Mirror;
using PluginAPI.Core;

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
            var key = ServerStatic.PermissionsHandler._groups.FirstOrDefault(kvp => kvp.Value == __instance.Group).Key;
            if (p == null || string.IsNullOrEmpty(i) || string.IsNullOrEmpty(key) || !(SCPStats.Singleton?.Config?.RoleNames ?? RoleNames).TryGetValue(key, out var value)) return true;

            var stats = EventHandler.UserInfo.TryGetValue(Helper.HandleId(p), out var info) && info?.Item2 != null ? info.Item2.Stats : Array.Empty<string>();

            var newName = _varRegex.Replace(value, match =>
            {
                // This should never happen, but it's here because of a bug.
                if (match.Groups.Count < 2) return match.Value;

                var statKey = match.Groups[1].Value;
                if (!Helper.Rankings.TryGetValue(statKey, out var idx)) return match.Value;

                var output = stats.Length <= idx ? "0" : stats[idx];

                if (!int.TryParse(output, out var seconds)) return output;

                switch (idx)
                {
                    //Playtime
                    case 3:
                    case 12:
                    case 13:
                    case 14:
                        output = Helper.SecondsToHours(seconds);
                        break;
                    //Escape time
                    case 10:
                        output = Helper.SecondsToString(seconds);
                        break;
                }

                return output;
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