// -----------------------------------------------------------------------
// <copyright file="GeneratingWarningMessageEventArgs.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Exiled.Events.EventArgs.Interfaces;
using SCPStats.Websocket.Data;

namespace SCPStats.API.EventArgs
{
    /// <summary>
    /// Includes all of the information before a warning message is generated.
    /// </summary>
    public class GeneratingWarningMessageEventArgs : IExiledEvent
    {
        internal GeneratingWarningMessageEventArgs(List<Warning> warnings, string initialMessage)
        {
            Warnings = warnings;
            InitialMessage = initialMessage;
        }

        /// <summary>
        /// The list of <see cref="Warnings"/> that will be used to generate the warning message.
        /// </summary>
        public List<Warning> Warnings { get; }
        
        /// <summary>
        /// The initial message (by default "ID | Type | Message | Ban Length\n\n").
        /// </summary>
        public string InitialMessage { get; set; }
    }
}