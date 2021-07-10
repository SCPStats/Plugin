using System.Collections.Generic;
using SCPStats.Websocket.Data;

namespace SCPStats.API.EventArgs
{
    /// <summary>
    /// Includes all of the information before a warning message is sent.
    /// </summary>
    public class SendingWarningMessageEventArgs : System.EventArgs
    {
        internal SendingWarningMessageEventArgs(List<Warning> warnings, string message)
        {
            Warnings = warnings;
            Message = message;
        }

        /// <summary>
        /// The list of <see cref="Warnings"/> that will be used to generate the warning message.
        /// </summary>
        public List<Warning> Warnings { get; }

        /// <summary>
        /// The message that will be sent.
        /// </summary>
        public string Message { get; set; }
    }
}