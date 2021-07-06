using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;
using SCPStats.Websocket.Data;

namespace SCPStats
{
    public class Translation : ITranslation
    {
        [Description("The message sent to users attempting to run a command they do not have permission to run.")]
        public string NoPermissionMessage { get; set; } = "You do not have permission to run this command!";
        
        [Description("The message sent when a non-player attempts to use command that can only be ran by players.")]
        public string NotPlayer { get; set; } = "This command can only be ran by a player!";

        [Description("The message sent when an error occurs.")]
        public string ErrorMessage { get; set; } = "An error occured. Please try again.";

        [Description("The kick message used when a player isn't whitelisted.")]
        public string WhitelistKickMessage { get; set; } = "[SCPStats] You are not whitelisted on this server!";
        
        [Description("The kick message used when a player is banned.")]
        public string BannedKickMessage { get; set; } = "[SCPStats] You have been banned from this server: You have a ban issued on another server linked to this one!";
        
        [Description("Uses to change the delete warning command.")]
        public string DeleteWarningCommand { get; set; } = "deletewarning";

        [Description("The description of the delete warning command.")]
        public string DeleteWarningDescription { get; set; } = "Delete a warning.";

        [Description("The message sent when a user uses the delete warning command incorrectly.")]
        public string DeleteWarningUsage { get; set; } = "Usage: deletewarning <id>";

        [Description("The message sent when the delete warning command is executed successfully.")]
        public string DeleteWarningSuccess { get; set; } = "Deleting warning...";

        [Description("The message sent when a warning is deleted successfully.")]
        public string WarningDeleted { get; set; } = "Successfully deleted warning!";
        
        [Description("Uses to change the warnings command.")]
        public string WarningsCommand { get; set; } = "warnings";

        [Description("The description of the warnings command.")]
        public string WarningsDescription { get; set; } = "View warnings on a specific player.";

        [Description("The message sent when a user uses the warnings command incorrectly.")]
        public string WarningsUsage { get; set; } = "Usage: warnings <id>";

        [Description("The message sent when the warnings command is executed successfully.")]
        public string WarningsSuccess { get; set; } = "Requesting warnings...";

        [Description("The message sent when the list of warnings is received.")]
        public string Warnings { get; set; } = "\nID | Type | Message | Ban Length\n\n";
        
        [Description("The name used for warnings in the warnings command.")]
        public string WarningsTypeWarning { get; set; } = "Warning";
        
        [Description("The name used for bans in the warnings command.")]
        public string WarningsTypeBan { get; set; } = "Ban";
        
        [Description("The name used for kicks in the warnings command.")]
        public string WarningsTypeKick { get; set; } = "Kick";
        
        [Description("The name used for mutes in the warnings command.")]
        public string WarningsTypeMute { get; set; } = "Mute";
        
        [Description("The name used for warnings in the warnings command.")]
        public string WarningsTypeIntercomMutes { get; set; } = "Intercom Mute";

        [Description("The types of warnings that will be displayed by the warnings command. Possible options are: \"Warning\", \"Ban\", \"Kick\", \"Mute\", and \"IntercomMute\".")]
        public List<WarningType> WarningsDisplayedTypes { get; set; } = new List<WarningType>()
        {
            WarningType.Warning,
            WarningType.Ban,
            WarningType.Kick,
            WarningType.Mute,
            WarningType.IntercomMute
        };

        [Description("The warning sections that will be displayed in each warning by the warning command. Possible options are: \"ID\", \"Type\", \"Message\", \"Length\", \"Issuer\".")]
        public List<WarningSection> WarningsDisplayedSections { get; set; } = new List<WarningSection>()
        {
            WarningSection.ID,
            WarningSection.Type,
            WarningSection.Message,
            WarningSection.Length
        };

        [Description("Should seconds be converted to years, months, days, etc instead of displaying directly as seconds.")]
        public bool WarningsPrettyPrintSeconds { get; set; } = true;

        [Description("The separator between warning sections in the warnings command.")]
        public string WarningsSectionSeparator { get; set; } = " | ";
        
        [Description("Uses to change the warn command.")]
        public string WarnCommand { get; set; } = "warn";

        [Description("The description of the warn command.")]
        public string WarnDescription { get; set; } = "Warn a player.";

        [Description("The message sent when a user uses the warn command incorrectly.")]
        public string WarnUsage { get; set; } = "Usage: warn <id> [reason]";

        [Description("The message sent when the player input was not found.")]
        public string WarnPlayerNotFound { get; set; } = "The specified player was not found! Use the owarn command to warn offline players.";

        [Description("The message sent when the warn command is executed successfully.")]
        public string WarnSuccess { get; set; } = "Added warning.";
        
        [Description("Uses to change the owarn command.")]
        public string OWarnCommand { get; set; } = "owarn";

        [Description("The description of the owarn command.")]
        public string OWarnDescription { get; set; } = "Warn an offline player.";

        [Description("The message sent when a user uses the owarn command incorrectly.")]
        public string OWarnUsage { get; set; } = "Usage: owarn <id> [reason]";

        [Description("The message sent when a user inputs an invalid ID in the owarn command.")]
        public string OWarnInvalidID { get; set; } = "Please enter a valid user id (for example, ID@steam)!";
        
        [Description("The message sent when a user inputs an ID that is too long in the owarn command.")]
        public string OWarnIDTooLong { get; set; } = "User IDs have a maximum length of 18 characters. The one you have input is larger than that!";
        
        [Description("The message sent when a user inputs an ID that contains non-numbers in the owarn command.")]
        public string OWarnIDNotNumeric { get; set; } = "User IDs cannot contain non-numbers!";

        [Description("The message sent when the owarn command is executed successfully.")]
        public string OWarnSuccess { get; set; } = "Added warning.";
        
        [Description("Uses to change the pause stats command.")]
        public string PauseStatsCommand { get; set; } = "pausestats";

        [Description("The description of the pause stats command.")]
        public string PauseStatsDescription { get; set; } = "Temporarily pause stat collection for the round. Useful for events.";

        [Description("The message sent when the pause stats command is executed successfully.")]
        public string PauseStatsSuccess { get; set; } = "Successfully paused stat collection for the round.";
        
        [Description("Uses to change the hat command.")]
        public string HatCommand { get; set; } = "hat";

        [Description("The description of the hat command.")]
        public string HatDescription { get; set; } = "Change your hat ingame. This only applies to the current round.";

        [Description("The message sent when a user uses the hat command incorrectly.")]
        public string HatUsage { get; set; } = "Usage: .hat <on/off/toggle/item>";
        
        [Description("The message sent when a user puts on their hat.")]
        public string HatEnabled { get; set; } = "You put on your hat.";
        
        [Description("The message sent when a user attempts to put on their hat while already wearing a hat.")]
        public string HatEnableFail { get; set; } = "You can't put two hats on at once!";
        
        [Description("The message sent when a user puts on their hat.")]
        public string HatDisabled { get; set; } = "You took off your hat.";
        
        [Description("The message sent when a user attempts to take off their hat while not wearing a hat.")]
        public string HatDisableFail { get; set; } = "You don't have a hat on. You need to put one on before you can take it off.";
        
        [Description("The message sent when a user requests a list of the available hats.")]
        public string HatList { get; set; } = "This hat doesn't exist! Available hats:";

        [Description("The message sent when a user changes their hat.")]
        public string HatChanged { get; set; } = "Your hat has been changed.";

        public string TimeSeconds { get; set; } = "second(s)";
        public string TimeMinutes { get; set; } = "minute(s)";
        public string TimeHours { get; set; } = "hour(s)";
        public string TimeDays { get; set; } = "day(s)";
        public string TimeWeeks { get; set; } = "week(s)";
        public string TimeMonths { get; set; } = "month(s)";
        public string TimeYears { get; set; } = "year(s)";
    }
}