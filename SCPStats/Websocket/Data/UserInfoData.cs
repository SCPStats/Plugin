// -----------------------------------------------------------------------
// <copyright file="UserInfoData.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;

namespace SCPStats.Websocket.Data
{
    public class UserInfoData
    {
        public bool IsBooster { get; }
        public bool IsDiscordMember { get; }
        public string[] DiscordRoles { get; }
        public string[] Ranks { get; }
        public string[] Stats { get; }
        public bool HasHat { get; }
        public string HatID { get; }
        public bool IsBanned { get; }
        public string WarnMessage { get; }

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
            WarnMessage = length > 8 && !string.IsNullOrEmpty(flags[8]) ? Encoding.UTF8.GetString(Convert.FromBase64String(flags[8])) : null;
        }
    }
}