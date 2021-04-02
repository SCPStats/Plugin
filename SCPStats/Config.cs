// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs;

namespace SCPStats
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("The Server ID for your server. You must register your server at https://scpstats.com to obtain this.")]
        public string ServerId { get; set; } = "fill this";
        
        [Description("The Secret for your server. This should be treated like a password. You must register your server at https://scpstats.com to obtain this.")]
        public string Secret { get; set; } = "fill this";

        [Description("Turning this off will disable the auto updater, which will automatically update the plugin.")]
        public bool AutoUpdates { get; set; } = true;

        [Description("The role that should be given to nitro boosters. Your server must be linked to your discord server to do this.")]
        public string BoosterRole { get; set; } = "none";

        [Description("The role that should be given to discord members. Your server must be linked to your discord server to do this.")]
        public string DiscordMemberRole { get; set; } = "none";

        [Description("Roles that you want to sync. Adding a role here means that if the person has the role on discord, they will get it in game. If a user has multiple roles that can be synced, the highest role in this list will be chosen. Your server must be linked to your discord server to do this. You can also give roles based on how the player ranks in certain stats. For example, you can give 20 players with the highest playtime a role with the example role. All of the possible metrics are: \"kills\", \"deaths\", \"rounds\", \"playtime\", \"sodas\", \"medkits\", \"balls\", \"adrenaline\".")]
        public List<string> RoleSync { get; set; } = new List<string>()
        {
            "DiscordRoleID:IngameRoleName",
            "playtime_20:IngameRoleName"
        };
        
        [Description("The whitelist will only allow a player to join the server if they meet certain conditions. See the below options for how to change the whitelist's behavior. The whitelist is just a list of the same conditions used in rolesync (so only the left side, without the : ), with the addition of \"discordmember\" and \"booster\" being valid conditions.")]
        public List<string> Whitelist { get; set; } = new List<string>()
        {
            "DiscordRoleID"
        };

        [Description("By default, the whitelist will allow a person in if they match any of the conditions. Setting this value to true will mean that a person will only be let in if every condition matches.")]
        public bool WhitelistRequireAll { get; set; } = false;

        [Description("SCPStats includes hats to give perks to its donators. If you want to reward your own donators with hats, you can give them the scpstats.hats permission.")]
        public bool EnableHats { get; set; } = true;

        [Description("SCPStats will send a message to players attempting to pick up hats informing them where they can go to get one themselves.")]
        public bool DisplayHatHint { get; set; } = true;

        [Description("If you enable this option, bans will automatically be synced across every server linked together.")]
        public bool SyncBans { get; set; } = false;

        [Description("Display a broadcast at the end of the round. You must be an SCPStats patreon supporter to use this feature. More information is available below.")]
        public bool RoundSummaryBroadcastEnabled { get; set; } = false;
        
        [Description("If enabled, this will display a broadcast on round end containing information about the game (such as who had the most kills and how many they had). In this, you can use variables that follow the format {type_metric_pos} or {num_type_metric_pos} (num means that it will display the value of the metric instad of the player), and can include a message if no one got any stats in the specified metric with {type_metric_pos;default message}. Type can be \"score\" or \"order\". Score sorts by their score, while order sorts by who did it first. Pos is the position in the leaderboard. For example, \"{score_kills_1;No one} got {num_score_kills_1;any} kills.\" will show the person who got the most kills and how many they got. Additionally, you can set the default message to \"|end|\" (or include it anywhere) and everything after the end will be removed.")]
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

        [Description("This can help solve problems, but will spam your console.")]
        public bool Debug { get; set; } = false;
    }
}