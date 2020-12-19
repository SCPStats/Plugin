using System.Collections.Generic;
using System.ComponentModel;
using Synapse.Config;

namespace SCPStats
{
    public class Config : AbstractConfigSection
    {
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

        [Description("Roles that you want to sync. Adding a role here means that if the person has the role on discord, they will get it in game. If a user has multiple roles that can be synced, the highest role in this list will be chosen. Your server must be linked to your discord server to do this.")]
        public List<string> RoleSync { get; set; } = new List<string>()
        {
            "DiscordRoleID:IngameRoleName"
        };

        [Description("SCPStats includes hats to give perks to its donators. If you want to reward your own donators with hats, you can give them the scpstats.hats permission.")]
        public bool EnableHats { get; set; } = true;

        [Description("This is the amount of time (in seconds) that hats positions and will be updated in. The lower the number, the smoother the hats will look, but it will also put more load on the server.")]
        public float HatUpdateTime { get; set; } = .4f;
    }
}