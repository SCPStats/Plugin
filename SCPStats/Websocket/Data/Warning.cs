﻿// -----------------------------------------------------------------------
// <copyright file="Warning.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

namespace SCPStats.Websocket.Data
{
    public class Warning
    {
        /// <summary>
        /// The ID of the warning.
        /// </summary>
        public int ID { get; set; } = -1;

        /// <summary>
        /// The type of the warning.
        /// </summary>
        public WarningType Type { get; set; } = WarningType.None;

        /// <summary>
        /// The message of the warning, in the case of a warn, kick, or ban.
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// The ID of the server that this warn was made on.
        /// </summary>
        public string Server { get; set; } = "";

        /// <summary>
        /// The length of the warning, in the case of a ban.
        /// </summary>
        public int Length { get; set; } = 0;

        /// <summary>
        /// The issuer of the warning, in the case of a warn, kick, or ban.
        /// </summary>
        public string Issuer { get; set; } = "";

        public Warning(string[] data)
        {
            if (data.Length > 0 && int.TryParse(data[0], out var id)) ID = id;
            if (data.Length > 1 && int.TryParse(data[1], out var type)) Type = (WarningType) type;
            if (data.Length > 2) Message = data[2];
            if (data.Length > 3) Server = data[3];
            if (data.Length > 4 && int.TryParse(data[4], out var length)) Length = length;
            if (data.Length > 5) Issuer = data[5];
        }
    }

    public enum WarningType
    {
        None = -1,
        Warning = 0,
        Ban = 1,
        Kick = 2,
        Mute = 3,
        IntercomMute = 4,
        Note = 5
    }

    public enum WarningSection
    {
        ID,
        Type,
        Message,
        Length,
        Issuer
    }
}