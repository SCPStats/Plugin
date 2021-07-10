// -----------------------------------------------------------------------
// <copyright file="Helper.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Exiled.API.Features;
using Exiled.Loader;
using UnityEngine;

namespace SCPStats
{
    internal static class Helper
    {
        internal static Dictionary<string, string> RoundSummaryMetrics = new Dictionary<string, string>()
        {
            {"kills", "Kills"},
            {"kill", "Kills"},
            {"playerkills", "PlayerKills"},
            {"playerkill", "PlayerKills"},
            {"scpkills", "ScpKills"},
            {"scpkill", "ScpKills"},
            {"deaths", "Deaths"},
            {"death", "Deaths"},
            {"dies", "Deaths"},
            {"die", "Deaths"},
            {"sodas", "Sodas"},
            {"soda", "Sodas"},
            {"cokes", "Sodas"},
            {"coke", "Sodas"},
            {"colas", "Sodas"},
            {"cola", "Sodas"},
            {"scp207s", "Sodas"},
            {"scp207", "Sodas"},
            {"scp-207s", "Sodas"},
            {"scp-207", "Sodas"},
            {"medkits", "Medkits"},
            {"medkit", "Medkits"},
            {"health", "Medkits"},
            {"healthpack", "Medkits"},
            {"healthpacks", "Medkits"},
            {"balls", "Balls"},
            {"ball", "Balls"},
            {"scp018s", "Balls"},
            {"scp018", "Balls"},
            {"scp18s", "Balls"},
            {"scp18", "Balls"},
            {"scp-018s", "Balls"},
            {"scp-018", "Balls"},
            {"scp-18s", "Balls"},
            {"scp-18", "Balls"},
            {"adrenaline", "Adrenaline"},
            {"adrenalines", "Adrenaline"},
            {"escape", "Escapes"},
            {"escapes", "Escapes"},
            {"leaderboard", "Xp"},
            {"leaderboards", "Xp"},
            {"xp", "Xp"},
            {"xps", "Xp"},
            {"fastestescape", "FastestEscape"},
            {"fastescape", "FastestEscape"},
            {"quickestescape", "FastestEscape"},
            {"quickescape", "FastestEscape"}
        };
        
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

        internal static bool IsPlayerTutorial(Player p)
        {
            var playerIsSh = ((List<Player>) Integrations.GetSH?.Invoke(null, null))?.Any(pl => pl.Id == p.Id) ?? false;

            return p.Role == RoleType.Tutorial && !playerIsSh && !IsPlayerGhost(p);
        }

        internal static PlayerInfo GetPlayerInfo(Player p, bool tutorial = true, bool spectator = true)
        {
            return p != null && (p.NoClipEnabled || p.IsGodModeEnabled || IsPlayerNPC(p) || (tutorial && IsPlayerTutorial(p)))
                ? new PlayerInfo(null, RoleType.None, false)
                : p?.UserId == null || p.IsHost || !p.IsVerified || (spectator && (p.Role == RoleType.None || p.Role == RoleType.Spectator))
                    ? new PlayerInfo(null, RoleType.None, true)
                    : p.DoNotTrack 
                        ? new PlayerInfo(null, p.Role, true) 
                        : new PlayerInfo(Helper.HandleId(p.UserId), p.Role, true);
        }

        internal static bool IsRoundRunning() => !EventHandler.PauseRound && RoundSummary.RoundInProgress();

        internal static string HmacSha256Digest(string secret, string message)
        {
            var encoding = new UTF8Encoding();
            
            return BitConverter.ToString(new HMACSHA256(encoding.GetBytes(secret)).ComputeHash(encoding.GetBytes(message))).Replace("-", "").ToLower();
        }
        
        internal static string HandleId(string id)
        {
            return id?.Split('@')[0];
        }

        internal static string HandleId(Player player)
        {
            return HandleId(player?.UserId);
        }

        internal static bool IsPlayerGhost(Player p)
        {
            return (bool) (Integrations.IsGhost?.Invoke(null, new object[] {p}) ?? false);
        }

        internal static bool IsPlayerNPC(Player p)
        {
            return (bool) (Integrations.IsNpc?.Invoke(null, new object[] {p}) ?? false) || p.Id == 9999 || p.IPAddress == "127.0.0.WAN";
        }
        
        internal static void SendWarningMessage(Player p, string reason){
            if(!string.IsNullOrEmpty(SCPStats.Singleton?.Config?.WarningMessage) && SCPStats.Singleton.Config.WarningMessage != "none" && SCPStats.Singleton.Config.WarningMessageDuration > 0) p.Broadcast(SCPStats.Singleton.Config.WarningMessageDuration, SCPStats.Singleton.Config.WarningMessage.Replace("{reason}", reason));
        }

        internal static bool IsZero(this Quaternion rot) => rot.x == 0 && rot.y == 0 && rot.z == 0;
    }
}