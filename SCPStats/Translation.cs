// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

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
        
        [Description("The kick message used when a player is banned. Use \"{duration}\" for the amount of time left until the ban expires and \"{reason}\" for the ban reason.")]
        public string BannedMessage { get; set; } = "[SCPStats] You have been banned from this server:\nExpires in: {duration}.\nReason: {reason}.";

        [Description("The kick message used when a player is banned but the full details of the ban could not be retrieved.")]
        public string CacheBannedMessage { get; set; } =
            "[SCPStats] You have been banned from this server, but there was an error fetching the details of your ban.";

        [Description("If RequireConfirmation is enabled, this message will be sent when an unconfirmed user joins.")]
        public string NotConfirmedKickMessage { get; set; } = "[SCPStats] An authentication error occured between the server and SCPStats! Please try again.";

        [Description("The message sent as an initial command response for commands that need to interact with SCPStats (warn, warnings, etc).")]
        public string PleaseWait { get; set; } = "Please wait...";
        
        [Description("Used to change the ip ban command.")]
        public string IpBanCommand { get; set; } = "ipban";

        [Description("Used to change the ip ban command aliases.")]
        public List<string> IpBanCommandAliases { get; set; } = new List<string>() {"banip"};

        [Description("The description of the ip ban command.")]
        public string IpBanDescription { get; set; } = "Ban an IP.";

        [Description("The message sent when a user uses the ip ban command incorrectly.")]
        public string IpBanUsage { get; set; } = "Usage: ipban <id> <duration> [reason]";

        [Description("Autocomplete usage for the ip ban command.")]
        public string[] IpBanUsages { get; set; } = new string[] {"id", "duration", "reason"};

        [Description("The message sent when a user enters an invalid ip.")]
        public string IpBanInvalidIp { get; set; } = "Please enter a valid IP (for example, 1.1.1.1)!";

        [Description("The message sent when a user is not allowed to ban a specific ip.")]
        public string IpBanCantBan { get; set; } = "You cannot ban this IP!";

        [Description("The message sent when an ip is successfully banned.")]
        public string IpBanSuccess { get; set; } = "Successfully banned IP!";

        [Description("Used to change the delete warning command.")]
        public string DeleteWarningCommand { get; set; } = "deletewarning";

        [Description("Used to change the delete warning command aliases.")]
        public List<string> DeleteWarningCommandAliases { get; set; } = new List<string>() {"deletewarnings", "delwarning", "delwarnings", "delwarn", "deletewarns", "deletewarn", "delwarns"};

        [Description("The description of the delete warning command.")]
        public string DeleteWarningDescription { get; set; } = "Delete a warning.";

        [Description("The message sent when a user uses the delete warning command incorrectly.")]
        public string DeleteWarningUsage { get; set; } = "Usage: deletewarning <id>";
        
        [Description("Autocomplete usage for the delete warning command.")]
        public string[] DeleteWarningUsages { get; set; } = new string[] {"id"};
        
        [Description("The message sent when a user inputs an ID that contains non-numbers in the delwarn command.")]
        public string DeleteWarningIdNotNumeric { get; set; } = "Warning IDs cannot contain non-numbers!";

        [Description("The message sent when a warning is deleted successfully.")]
        public string WarningDeleted { get; set; } = "Successfully deleted warning!";
        
        [Description("Used to change the warnings command.")]
        public string WarningsCommand { get; set; } = "warnings";

        [Description("Used to change the warnings command aliases.")]
        public List<string> WarningsCommandAliases { get; set; } = new List<string>() {"warning", "warns", "getwarns", "getwarnings"};

        [Description("The description of the warnings command.")]
        public string WarningsDescription { get; set; } = "View warnings on a specific player.";

        [Description("The message sent when a user uses the warnings command incorrectly.")]
        public string WarningsUsage { get; set; } = "Usage: warnings <id>";

        [Description("Autocomplete usage for the warnings command.")]
        public string[] WarningsUsagesList { get; set; } = new string[] {"%player%"};

        [Description("The message sent when the list of warnings is received.")]
        public string Warnings { get; set; } = "\nID | Type | Message | Ban Length\n\n";
        
        [Description("The name used for warnings in the warnings command.")]
        public string WarningsTypeWarning { get; set; } = "Warning";
        
        [Description("The name used for notes in the warnings command.")]
        public string WarningsTypeNote { get; set; } = "Note";
        
        [Description("The name used for bans in the warnings command.")]
        public string WarningsTypeBan { get; set; } = "Ban";
        
        [Description("The name used for kicks in the warnings command.")]
        public string WarningsTypeKick { get; set; } = "Kick";

        [Description("The name used for mutes in the warnings command.")]
        public string WarningsTypeMute { get; set; } = "Mute";

        [Description("The name used for warnings in the warnings command.")]
        public string WarningsTypeIntercomMutes { get; set; } = "Intercom Mute";

        [Description("The types of warnings that will be displayed by the warnings command. Possible options are: \"Warning\", \"Note\", \"Ban\", \"Kick\", \"Mute\", and \"IntercomMute\".")]
        public List<WarningType> WarningsDisplayedTypes { get; set; } = new List<WarningType>()
        {
            WarningType.Warning,
            WarningType.Note,
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
        
        [Description("Used to change the warn command.")]
        public string WarnCommand { get; set; } = "warn";

        [Description("Used to change the warn command aliases.")]
        public List<string> WarnCommandAliases { get; set; } = new List<string>() {};

        [Description("The description of the warn command.")]
        public string WarnDescription { get; set; } = "Warn a player.";

        [Description("The message sent when a user uses the warn command incorrectly.")]
        public string WarnUsage { get; set; } = "Usage: warn <id> [reason]";

        [Description("Autocomplete usage for the warn command.")]
        public string[] WarnUsagesList { get; set; } = new string[] {"%player%", "reason"};

        [Description("The message sent when a user inputs an invalid ID in the owarn command.")]
        public string WarnInvalidId { get; set; } = "Please enter a valid user id (for example, ID@steam)!";
        
        [Description("The message sent when a user inputs an ID that is too long in the owarn command.")]
        public string WarnIdTooLong { get; set; } = "User IDs have a maximum length of 18 characters. The one you have input is larger than that!";
        
        [Description("The message sent when a user inputs an ID that contains non-numbers in the owarn command.")]
        public string WarnIdNotNumeric { get; set; } = "User IDs cannot contain non-numbers!";

        [Description("The message sent when the warn command is executed successfully.")]
        public string WarnSuccess { get; set; } = "Added warning.";

        [Description("Used to change the silent warn command.")]
        public string SilentWarnCommand { get; set; } = "swarn";

        [Description("Used to change the silent command aliases.")]
        public List<string> SilentWarnCommandAliases { get; set; } = new List<string>() {"silentwarn"};

        [Description("The description of the silent warn command.")]
        public string SilentWarnDescription { get; set; } = "Silently warn a player (without showing a message on their screen).";

        [Description("The message sent when a user uses the silent warn command incorrectly.")]
        public string SilentWarnUsage { get; set; } = "Usage: swarn <id> [reason]";
        
        [Description("Autocomplete usage for the silent warn command.")]
        public string[] SilentWarnUsagesList { get; set; } = new string[] {"%player%", "reason"};

        [Description("The message sent when the warn command is executed successfully.")]
        public string SilentWarnSuccess { get; set; } = "Added warning.";
        
        [Description("Used to change the note command.")]
        public string NoteCommand { get; set; } = "note";

        [Description("Used to change the note aliases.")]
        public List<string> NoteCommandAliases { get; set; } = new List<string>() {};

        [Description("The description of the note command.")]
        public string NoteDescription { get; set; } = "Create a note about a player.";

        [Description("The message sent when a user uses the note command incorrectly.")]
        public string NoteUsage { get; set; } = "Usage: note <id> [message]";
        
        [Description("Autocomplete usage for the note command.")]
        public string[] NoteUsagesList { get; set; } = new string[] {"%player%", "message"};

        [Description("The message sent when the note is executed successfully.")]
        public string NoteSuccess { get; set; } = "Added note.";

        [Description("Used to change the pause stats command.")]
        public string PauseStatsCommand { get; set; } = "pausestats";

        [Description("Used to change the pause stats command aliases.")]
        public List<string> PauseStatsCommandAliases { get; set; } = new List<string>() {"pausestat", "pausescpstats", "pausescpstat", "pauseround"};

        [Description("The description of the pause stats command.")]
        public string PauseStatsDescription { get; set; } = "Temporarily pause stat collection for the round. Useful for events.";

        [Description("The message sent when the pause stats command is executed successfully.")]
        public string PauseStatsSuccess { get; set; } = "Successfully paused stat collection for the round.";
        
        [Description("Used to change the hat command.")]
        public string HatCommand { get; set; } = "hat";

        [Description("Used to change the hat command aliases.")]
        public List<string> HatCommandAliases { get; set; } = new List<string>() { "hats" };

        [Description("The description of the hat command.")]
        public string HatDescription { get; set; } = "Change your hat ingame. This only applies to the current round.";

        [Description("The message sent when a user uses the hat command incorrectly.")]
        public string HatUsage { get; set; } = "Usage: .hat <on/off/toggle/default/item>";

        [Description("Autocomplete usage for the hat command.")]
        public string[] HatUsages { get; set; } = new string[] {"on/off/toggle/default/item"};

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

        [Description("The message sent when a user changes their hat back to their default hat.")]
        public string HatDefault { get; set; } = "Your hat has been changed back to your default hat.";

        [Description("The hint shown when a person tries to pick up someone else's hat.")]
        public string HatHint { get; set; } = "You can get a hat like this at patreon.com/SCPStats.";

        [Description("The message shown when a player uses the hat command while hats are disabled.")]
        public string HatsNotEnabled { get; set; } = "Hats are not enabled on this server.";
        
        [Description("Used to change the stats command.")]
        public string StatsCommand { get; set; } = "stats";

        [Description("Used to change the stats command aliases.")]
        public List<string> StatsCommandAliases { get; set; } = new List<string>() {"stat", "serverstat", "serverstats"};

        [Description("The description of the stats command.")]
        public string StatsDescription { get; set; } = "View your stats and ranking for this server.";

        [Description("The header sent with the stats command response.")]
        public string StatsHeader { get; set; } = "\nHere are your stats for this server:\n\nStat - Amount - Rank";

        [Description("The message sent when someone doesn't have any stats.")]
        public string StatsNoStats { get; set; } =
            "Looks like you don't have any stats. Make sure you've signed up at https://scpstats.com to view your stats.";

        [Description("The translations for the different stats returned by the stats command.")]
        public string StatsKD { get; set; } = "K/D";
        public string StatsKills { get; set; } = "Kills";
        public string StatsDeaths { get; set; } = "Deaths";
        public string StatsPlaytime { get; set; } = "Playtime";
        public string StatsRounds { get; set; } = "Rounds Played";
        public string StatsSodas { get; set; } = "Sodas Consumed";
        public string StatsEscapes { get; set; } = "Escapes";
        public string StatsFastestEscape { get; set; } = "Fastest Escape";

        public string TimeSeconds { get; set; } = "second(s)";
        public string TimeMinutes { get; set; } = "minute(s)";
        public string TimeHours { get; set; } = "hour(s)";
        public string TimeDays { get; set; } = "day(s)";
        public string TimeWeeks { get; set; } = "week(s)";
        public string TimeMonths { get; set; } = "month(s)";
        public string TimeYears { get; set; } = "year(s)";
    }
}