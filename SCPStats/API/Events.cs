// -----------------------------------------------------------------------
// <copyright file="Events.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

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
        public static System.EventHandler<UserInfoEventArgs> UserInfoReceived;

        /// <summary>
        /// Called after user info has been handled (after bans and rolesync, etc). This will not run if the player has been kicked.
        /// </summary>
        public static System.EventHandler<UserInfoEventArgs> UserInfoHandled;

        /// <summary>
        /// Called before a warning message is generated for the warnings command.
        /// </summary>
        public static System.EventHandler<GeneratingWarningMessageEventArgs> GeneratingWarningMessage;

        /// <summary>
        /// Called before a warning message is sent for the warnings command.
        /// </summary>
        public static System.EventHandler<SendingWarningMessageEventArgs> SendingWarningMessage;

        internal static void OnUserInfoReceived(UserInfoEventArgs ev) => UserInfoReceived?.Invoke(null, ev);

        internal static void OnUserInfoHandled(UserInfoEventArgs ev) => UserInfoHandled?.Invoke(null, ev);

        internal static void OnGeneratingWarningMessage(GeneratingWarningMessageEventArgs ev) => GeneratingWarningMessage?.Invoke(null, ev);

        internal static void OnSendingWarningMessage(SendingWarningMessageEventArgs ev) => SendingWarningMessage?.Invoke(null, ev);
    }
}