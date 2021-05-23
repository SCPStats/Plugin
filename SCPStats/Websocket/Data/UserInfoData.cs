// -----------------------------------------------------------------------
// <copyright file="UserInfoData.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using UnityEngine;

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
        public Vector3 HatScale { get; }
        public Vector3 HatOffset { get; }
        public Quaternion HatRotation { get; }
        public bool IsCustomHat { get; }

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
            HatScale = length > 11 && !string.IsNullOrEmpty(flags[9]) && !string.IsNullOrEmpty(flags[10]) && !string.IsNullOrEmpty(flags[11]) ? new Vector3(float.Parse(flags[9]), float.Parse(flags[10]), float.Parse(flags[11])) : Vector3.zero;
            HatOffset = length > 14 && !string.IsNullOrEmpty(flags[12]) && !string.IsNullOrEmpty(flags[13]) && !string.IsNullOrEmpty(flags[14]) ? new Vector3(float.Parse(flags[12]), float.Parse(flags[13]), float.Parse(flags[14])) : Vector3.zero;
            HatRotation = length > 17 && !string.IsNullOrEmpty(flags[15]) && !string.IsNullOrEmpty(flags[16]) && !string.IsNullOrEmpty(flags[17]) ? Quaternion.Euler(float.Parse(flags[15]), float.Parse(flags[16]), float.Parse(flags[17])) : Quaternion.identity;
            IsCustomHat = HatScale != Vector3.zero || HatOffset != Vector3.zero || HatRotation != Quaternion.identity;
        }
    }
}