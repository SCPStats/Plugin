using System.ComponentModel;
using Exiled.API.Interfaces;

namespace SCPStats
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("The Server ID for your server. You must register your server at https://scpstats.com to obtain this.")]
        public string ServerId { get; set; } = "fill this";
        
        [Description("The Secret for your server. This should be treated like a password. You must register your server at https://scpstats.com to obtain this.")]
        public string Secret { get; set; } = "fill this";
    }
}