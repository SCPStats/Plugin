namespace SCPStats.Websocket.Data
{
    public struct UserInfoData
    {
        public bool IsBooster { get; private set; }
        public bool IsDiscordMember { get; private set; }
        public string[] DiscordRoles { get; private set; }
        public string[] Ranks { get; private set; }
        public string[] Stats { get; private set; }
        public bool HasHat { get; private set; }
        public string HatID { get; private set; }
        public bool IsBanned { get; private set; }
        public string WarnMessage { get; private set; }

        public UserInfoData(string[] flags)
        {
            var length = flags.Length;

            IsBooster = length > 0 && flags[0] == "1";
            IsDiscordMember = length > 1 && flags[1] == "1";
            DiscordRoles = length > 2 && flags[2] != "0" ? flags[2].Split('|') : new string[]{};
            HasHat = length > 3 && flags[3] == "1";
            HatID = length > 4 ? flags[4] : "-1";
            Ranks = length > 5 && flags[5] != "0" ? flags[5].Split('|') : new string[]{};
            Stats = length > 6 && flags[6] != "0" ? flags[6].Split('|') : new string[]{};
            IsBanned = length > 7 && flags[7] == "1";
            WarnMessage = length > 8 && !string.IsNullOrEmpty(flags[8]) ? flags[8] : null;
        }
    }
}