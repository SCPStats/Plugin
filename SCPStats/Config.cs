using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace SCPStats.Commands
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("The Server ID for your server. You must register your server at https://scpstats.com to obtain this.")]
        public string ServerId { get; set; } = "fill this";
        
        [Description("The Secret for your server. This should be treated like a password. You must register your server at https://scpstats.com to obtain this.")]
        public string Secret { get; set; } = "fill this";

        [Description("Enabling this will create a separate config file (located next to the plugin config in the SCPStats directory) for SCPStats' Server ID and Secret. This can help you keep them secure, as well as make updating servers much easier. If you host multiple servers, it is highly recommended that you use this option.")]
        public bool SeparateConfig { get; set; } = false;

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

        [Description("SCPStats includes hats to give perks to its donators. If you want to reward your own donators with hats, you can give them the scpstats.hats permission.")]
        public bool EnableHats { get; set; } = true;

        [Description("This is the amount of time (in seconds) that hats positions and will be updated in. The lower the number, the smoother the hats will look, but it will also put more load on the server.")]
        public float HatUpdateTime { get; set; } = .4f;

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
    }
}