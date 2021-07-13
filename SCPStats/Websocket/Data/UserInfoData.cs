// -----------------------------------------------------------------------
// <copyright file="UserInfoData.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SCPStats.Websocket.Data
{
    public class UserInfoData
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

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

            HatScale = length > 11
                       && !string.IsNullOrEmpty(flags[9]) && !string.IsNullOrEmpty(flags[10]) && !string.IsNullOrEmpty(flags[11])
                       && float.TryParse(flags[9], NumberStyles.Float, UsCulture, out var hatScaleX) && float.TryParse(flags[10], NumberStyles.Float, UsCulture, out var hatScaleY) && float.TryParse(flags[11], NumberStyles.Float, UsCulture, out var hatScaleZ)
                ? new Vector3(hatScaleX, hatScaleY, hatScaleZ) 
                : Vector3.zero;
            HatOffset = length > 14
                       && !string.IsNullOrEmpty(flags[12]) && !string.IsNullOrEmpty(flags[13]) && !string.IsNullOrEmpty(flags[14])
                       && float.TryParse(flags[12], NumberStyles.Float, UsCulture, out var hatOffsetX) && float.TryParse(flags[13], NumberStyles.Float, UsCulture, out var hatOffsetY) && float.TryParse(flags[14], NumberStyles.Float, UsCulture, out var hatOffsetZ)
                ? new Vector3(hatOffsetX, hatOffsetY, hatOffsetZ) 
                : Vector3.zero;
            HatRotation = length > 17
                        && !string.IsNullOrEmpty(flags[15]) && !string.IsNullOrEmpty(flags[16]) && !string.IsNullOrEmpty(flags[17])
                        && float.TryParse(flags[15], NumberStyles.Float, UsCulture, out var hatRotationX) && float.TryParse(flags[16], NumberStyles.Float, UsCulture, out var hatRotationY) && float.TryParse(flags[17], NumberStyles.Float, UsCulture, out var hatRotationZ)
                ? Quaternion.Euler(hatRotationX, hatRotationY, hatRotationZ) 
                : Quaternion.identity;

            IsCustomHat = HatScale != Vector3.zero || HatOffset != Vector3.zero || !HatRotation.IsZero();
        }
    }
}