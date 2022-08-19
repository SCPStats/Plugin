// -----------------------------------------------------------------------
// <copyright file="UserInfoEventArgs.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Features;
using Exiled.Events.EventArgs.Interfaces;
using SCPStats.Websocket.Data;

namespace SCPStats.API.EventArgs
{
    /// <summary>
    /// Includes all of the information for user info events.
    /// </summary>
    public class UserInfoEventArgs : IExiledEvent
    {
        internal UserInfoEventArgs(Player player, UserInfoData userInfo, CentralAuthPreauthFlags? flags)
        {
            Player = player;
            UserInfo = userInfo;
            Flags = flags;
        }

        /// <summary>
        /// The <see cref="Player"/> associated with the user info. 
        /// </summary>
        public Player Player { get; }
        
        /// <summary>
        /// The <see cref="UserInfoData"/>, containing information about a player such as their stats and discord roles.
        /// </summary>
        public UserInfoData UserInfo { get; }
        
        /// <summary>
        /// The <see cref="CentralAuthPreauthFlags"/> sent by the player during preauth.
        /// </summary>
        public CentralAuthPreauthFlags? Flags { get; }
    }
}