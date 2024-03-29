﻿// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs;
using SCPStats.Hats;
using UnityEngine;

namespace SCPStats
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("Turning this off will disable the auto updater, which will automatically update the plugin.")]
        public bool AutoUpdates { get; set; } = true;

        [Description("If you enable this option, bans will automatically be synced across all of your servers.")]
        public bool SyncBans { get; set; } = false;

        [Description("If you enable this, bans will only be saved to SCPStats and will not be saved to the bans file. This fixes a bug where players must be unbanned on the server they were banned on, or else they will be unbanned on every server but that one. This does not affect already existing bans.")]
        public bool DisableBasegameBans { get; set; } = false;

        [Description("If you enable this, any normal bans that you issue (bans issued through the ban command, not the ipban command) will associate the banned player's IP along with their ID in the ban record (preventing the player from joining with the same ID or IP). This does not create a separate IP ban, so the process for unbanning a player is exactly the same as it is with this option off.")]
        public bool LinkIpsToBans { get; set; } = false;

        [Description("By default, SCPStats records basegame moderations such as kicks, bans, and reports automatically. Turning this off will stop SCPStats from recording these. Only turn this off if you aren't using SCPStats for moderation and don't plan to. Turning this off will not disable manual actions such as warns and ipbans (which are a part of SCPStats and not basegame).")]
        public bool ModerationLogging { get; set; } = true;

        [Description("The delay (in seconds) for preauth requests on the first round of the game.")]
        public byte FirstRoundPreauthDelay { get; set; } = 4;

        [Description("The role that should be given to nitro boosters. Your server must be linked to your discord server to do this.")]
        public string BoosterRole { get; set; } = "none";

        [Description("The role that should be given to discord members. Your server must be linked to your discord server to do this.")]
        public string DiscordMemberRole { get; set; } = "none";

        [Description("Roles that you want to sync. Adding a role here means that if the person has the role on discord, they will get it in game. If a user has multiple roles that can be synced, the highest role in this list will be chosen. Your server must be linked to your discord server to do this. You can also give roles based on how the player ranks in certain stats. For example, you can give 20 players with the highest playtime a role with the example role. All of the possible metrics are: \"kills\", \"deaths\", \"rounds\", \"playtime\", \"sodas\", \"medkits\", \"balls\", \"adrenaline\", \"escapes\", \"xp\", \"fastestescape\", \"level\", \"playtime30\", \"playtime7\", \"playtime1\", \"wins\", \"loses\", and \"pocketescapes\".")]
        public List<string> RoleSync { get; set; } = new List<string>()
        {
            "DiscordRoleID:IngameRoleName",
            "playtime_20:IngameRoleName"
        };

        [Description("Allows you to modify the name of a role, per-person. For example, you could modify a role's name to include the xp and level of of the person who has it. The key (left value) is the name of the role (not what's displayed ingame, but what's used in the remote admin configs). The value (right value) is the new name for the role. Inside of it, you can place metrics, surrounded by a \"{\" and \"}\" on either side. For example, \"{xp} XP\". See above for information on the available values for this.")]
        public Dictionary<string, string> RoleNames { get; set; } = new Dictionary<string, string>()
        {
            {"ExampleXPRole", "{xp} XP | Level {level}"}
        };

        public static bool WhitelistEnabled() => SCPStats.Singleton.Config?.Whitelist != null && SCPStats.Singleton.Config.Whitelist.Count(req => req != "DiscordRoleID") > 0;

        [Description("The whitelist will only allow a player to join the server if they meet certain conditions. See the below options for how to change the whitelist's behavior. The whitelist is just a list of the same conditions used in rolesync (so only the left side, without the : ), with the addition of \"discordmember\" and \"booster\" being valid conditions.")]
        public List<string> Whitelist { get; set; } = new List<string>()
        {
            "DiscordRoleID"
        };

        [Description("By default, the whitelist will allow a person in if they match any of the conditions. Setting this value to true will mean that a person will only be let in if every condition matches.")]
        public bool WhitelistRequireAll { get; set; } = false;

        [Description("Reserved slots will allow a player to join a full server, but only if they meet certain conditions. See the below options for how to change the reserved slots list' behavior. The reserved slots list is just a list of the same conditions used in rolesync (so only the left side, without the : ), with the addition of \"discordmember\" and \"booster\" being valid conditions.")]
        public List<string> ReservedSlots { get; set; } = new List<string>()
        {
            "DiscordRoleID"
        };

        [Description("By default, a player will have a reserved slot if they match any of the conditions. Setting this value to true will mean that a person will only be let in if every condition matches.")]
        public bool ReservedSlotsRequireAll { get; set; } = false;

        [Description("SCPStats includes hats to give perks to its donators. If you want to reward your own donators with hats, you can give them the scpstats.hats permission.")]
        public bool EnableHats { get; set; } = true;

        [Description("SCPStats will send a message to players attempting to pick up hats informing them where they can go to get one themselves.")]
        public bool DisplayHatHint { get; set; } = true;

        [Description("Display a broadcast at the end of the round. You must be an SCPStats patreon supporter to use this feature. More information is available below.")]
        public bool RoundSummaryBroadcastEnabled { get; set; } = false;

        [Description("If enabled, this will display a broadcast on round end containing information about the game (such as who had the most kills and how many they had). In this, you can use variables that follow the format {type_metric_pos} or {num_type_metric_pos} (num means that it will display the value of the metric instad of the player), and can include a message if no one got any stats in the specified metric with {type_metric_pos;default message}. Type can be \"score\" or \"order\". Score sorts by their score, while order sorts by who did it first. Pos is the position in the leaderboard. For example, \"{score_kills_1;No one} got {num_score_kills_1;any} kills.\" will show the person who got the most kills and how many they got. Additionally, you can set the default message to \"|end|\" (or include it anywhere) and everything after the end will be removed. Finally, you can split this up into \"pages\", where each page is either a new console entry, or broadcast (in this case, the length of it will be the broadcast length divided by the total amount of pages). Pages are created by putting \"|page|\" between two strings, and \"|pageend|\" can be used to end the current page, but not the entire message.")]
        public string RoundSummaryBroadcast { get; set; } = "{score_kills_1;No one} got {num_score_kills_1;any} kills.";

        [Description("How long the round summary broadcast should last.")]
        public ushort RoundSummaryBroadcastDuration { get; set; } = 3;
        
        [Description("Display a console message at the end of the round. You must be an SCPStats patreon supporter to use this feature.")]
        public bool RoundSummaryConsoleMessageEnabled { get; set; } = false;

        [Description("This field is exactly the same as RoundSummaryBroadcast, but sends a message to player's console instead of as a broadcast.")]
        public string RoundSummaryConsoleMessage { get; set; } = "{score_kills_1;No one} got {num_score_kills_1;any} kills.";

        [Description("What color the round summary console message is.")]
        public string RoundSummaryConsoleMessageColor { get; set; } = "yellow";

        [Description("Send a message to people when they are warned. Set to \"none\" to disable. \"{reason}\" will be replaced with the warning reason.")]
        public string WarningMessage { get; set; } = "You have been warned. Reason: {reason}";

        [Description("How long the above warning message should be.")]
        public ushort WarningMessageDuration { get; set; } = 5;

        [Description("If a kick message starts with one of these, it will not be recorded.")]
        public List<string> IgnoredMessages { get; set; } = new List<string>()
        {
            "[SCPStats]",
            "VPNs and proxies are forbidden",
            "<size=70><color=red>You are banned.",
            "Your account must be at least",
            "You have been banned.",
            "[Kicked by uAFK]",
            "You were AFK",
            "[Anty-AFK]",
            "[Anty AFK]",
            "Auto-Kick:",
            "[Auto-Kick]",
            "[Auto Kick]",
            "[Ultimate-AFK]",
            "You were kicked by a plugin"
        };

        [Description("Should SCPStats record stats? It is recommened to disable stat tracking on event/gamemode servers.")]
        public bool DisableRecordingStats { get; set; } = false;

        [Description("If enabled, IDs typed into commands will no longer be required to end with @steam/@discord/@northwood.")]
        public bool DisableIdAuthCheck { get; set; } = false;

        [Description("Should player names be sent on join? Enabling this will make server status messages display player names.")]
        public bool SendPlayerNames { get; set; } = false;

        [Description("Should each default hat require a separate permission to use? If this is enabled, the permission to give a hat is \"scpstats.hat.\" plus the lowercase name displayed in the command with all dashes removes (ex. SCP-268 -> scpstats.hat.scp268).")]
        public bool PerHatPermissions { get; set; } = false;
        
        [Description("Create custom hats which can be used in the .hat command. You can modify the item, scale, rotation, and offset of the hat, and define a permission that's required to use it. Supplying \"none\" for the permission means that it is usable by everyone.")]
        public Dictionary<string, CustomHat> Hats { get; set; } = new Dictionary<string, CustomHat>()
        {
            {"Example Hat", new CustomHat()
            {
                Item = ItemType.SCP268,
                Scale = Vector3.one,
                Offset = Vector3.zero,
                Rotation = Vector3.zero,
                Permission = "scpstats.hat.example"
            }}
        };

        [Description("This can help solve problems, but will spam your console.")]
        public bool Debug { get; set; } = false;
    }

    public struct CustomHat
    {
        public ItemType Item { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Offset { get; set; }
        public Vector3 Rotation { get; set; }
        public string Permission { get; set; }

        public HatInfo Info() => new HatInfo(Item, Scale, Offset, Quaternion.Euler(Rotation));
    }
}
