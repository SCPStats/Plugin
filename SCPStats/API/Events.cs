// -----------------------------------------------------------------------
// <copyright file="Events.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.Events.Extensions;
using SCPStats.API.EventArgs;

namespace SCPStats.API
{
    /// <summary>
    /// Contains all of SCPStats' events.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Called when a player joins, before user info is handled.
        /// </summary>
        public static Exiled.Events.Events.CustomEventHandler<UserInfoEventArgs> UserInfoReceived;
        
        /// <summary>
        /// Called after user info has been handled (after bans and rolesync, etc). This will not run if the player has been kicked.
        /// </summary>
        public static Exiled.Events.Events.CustomEventHandler<UserInfoEventArgs> UserInfoHandled;

        internal static void OnUserInfoReceived(UserInfoEventArgs ev) => UserInfoReceived.InvokeSafely(ev);
        
        internal static void OnUserInfoHandled(UserInfoEventArgs ev) => UserInfoHandled.InvokeSafely(ev);
    }
}