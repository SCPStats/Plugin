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
    /// <summary>
    /// Contains all of the information about a user that SCPStats will use to do certain actions when they join.
    /// </summary>
    public class UserInfoData
    {
        /// <summary>
        /// Is this user a nitro booster on any discord servers linked to this server?
        /// </summary>
        public bool IsBooster { get; }

        /// <summary>
        /// Is this user in discord servers linked to this server?
        /// </summary>
        public bool IsDiscordMember { get; }

        /// <summary>
        /// An array of role IDs that this user has on any discord servers linked to this server.
        /// </summary>
        public string[] DiscordRoles { get; }

        /// <summary>
        /// A string array of the position of this user in several of SCPStats' server-specific leaderboards.
        /// </summary>
        public string[] Ranks { get; }

        /// <summary>
        /// A string array of the server-stats that this user has.
        /// </summary>
        public string[] Stats { get; }

        /// <summary>
        /// Does this user have an SCPStats hat?
        /// </summary>
        public bool HasHat { get; }

        /// <summary>
        /// The item ID of the hat that this user has, if they have one.
        /// </summary>
        public string HatID { get; }

        /// <summary>
        /// Is this user banned?
        /// </summary>
        public bool IsBanned { get; }

        /// <summary>
        /// The reason why this user was banned, if they are banned.
        /// </summary>
        public string BanText { get; }

        /// <summary>
        /// The length (in seconds) of this user's ban, if they are banned.
        /// </summary>
        public int BanLength { get; }

        /// <summary>
        /// The warning message that will be broadcast to this user when they join, if they have one waiting to be sent.
        /// </summary>
        public string WarnMessage { get; }

        /// <summary>
        /// The scale of this user's hat.
        /// </summary>
        public Vector3 HatScale { get; }

        /// <summary>
        /// The offset of this user's hat.
        /// </summary>
        public Vector3 HatOffset { get; }

        /// <summary>
        /// The rotation of this user's hat.
        /// </summary>
        public Quaternion HatRotation { get; }

        /// <summary>
        /// Is this user's hat custom (does it have a custom scale, offset, or rotation)?
        /// </summary>
        public bool IsCustomHat { get; }
        
        /// <summary>
        /// Is this user's hat hidden from them (but visible to everyone else)?
        /// </summary>
        public bool HideHat { get; }

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
                       && float.TryParse(flags[9], NumberStyles.Float, Helper.UsCulture, out var hatScaleX) && float.TryParse(flags[10], NumberStyles.Float, Helper.UsCulture, out var hatScaleY) && float.TryParse(flags[11], NumberStyles.Float, Helper.UsCulture, out var hatScaleZ)
                ? new Vector3(hatScaleX, hatScaleY, hatScaleZ) 
                : Vector3.one;
            HatOffset = length > 14
                       && !string.IsNullOrEmpty(flags[12]) && !string.IsNullOrEmpty(flags[13]) && !string.IsNullOrEmpty(flags[14])
                       && float.TryParse(flags[12], NumberStyles.Float, Helper.UsCulture, out var hatOffsetX) && float.TryParse(flags[13], NumberStyles.Float, Helper.UsCulture, out var hatOffsetY) && float.TryParse(flags[14], NumberStyles.Float, Helper.UsCulture, out var hatOffsetZ)
                ? new Vector3(hatOffsetX, hatOffsetY, hatOffsetZ) 
                : Vector3.zero;
            HatRotation = length > 17
                        && !string.IsNullOrEmpty(flags[15]) && !string.IsNullOrEmpty(flags[16]) && !string.IsNullOrEmpty(flags[17])
                        && float.TryParse(flags[15], NumberStyles.Float, Helper.UsCulture, out var hatRotationX) && float.TryParse(flags[16], NumberStyles.Float, Helper.UsCulture, out var hatRotationY) && float.TryParse(flags[17], NumberStyles.Float, Helper.UsCulture, out var hatRotationZ)
                ? Quaternion.Euler(hatRotationX, hatRotationY, hatRotationZ) 
                : Quaternion.identity;

            IsCustomHat = HatScale != Vector3.one || HatOffset != Vector3.zero || !HatRotation.IsZero();

            BanText = length > 18 ? flags[18] : "";
            BanLength = length > 19 && int.TryParse(flags[19], NumberStyles.Integer, Helper.UsCulture, out var banLength) ? banLength : 0;

            HideHat = length > 20 && flags[20] == "1";
        }
    }
}