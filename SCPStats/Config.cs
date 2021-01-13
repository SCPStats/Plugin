using System.Collections.Generic;

namespace SCPStats
{
    public class Config
    {
        public bool IsEnabled { get; set; } = true;
        
        public string ServerId { get; set; } = "fill this";
        
        public string Secret { get; set; } = "fill this";
        
        public bool AutoUpdates { get; set; } = true;
        
        public string BoosterRole { get; set; } = "none";
        
        public string DiscordMemberRole { get; set; } = "none";
        
        public List<string> RoleSync { get; set; } = new List<string>()
        {
            "DiscordRoleID:IngameRoleName"
        };
        
        public bool EnableHats { get; set; } = true;
        
        public float HatUpdateTime { get; set; } = .4f;
    }
}