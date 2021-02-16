# SCPStats
SCPStats is a stat tracking plugin for SCP: Secret Laboratory that allows players to track their stats across multiple servers, as well as providing server hosts with detailed server analytics.

## Getting Started
Currently, SCPStats is WIP but is able to record player and server stats. This beta version can be installed to provide your players with stats when SCPStats fully releases. To use SCPStats, you first need to register your server with the website.

## Registering your Server
The SCPStats website is very WIP and does not have much effort put into actual the design of the site. SCPStat's full release will include a redesigned website.

To register your server, first login to SCPStats through https://scpstats.com/login . Then, register your server through https://scpstats.com/account/servers/add/ (you can register as many servers as you want). Once your server is registered, you will be redirected to a page where you can view all of your registered servers, along with their Server ID and Secret. You will need the Server ID and Secret to configure the SCPStats plugin.

## Configuration
Once you have your Server ID and Secret, configuration is simple. Simply put your Server ID and Secret under the relevent configuration options in your server's config. That's it. SCPStats will begin recording stats.

## Commands
SCPStats has the following commands:

* `warn <id> [message]` - `scpstats.warn` - Warn a person (with an optional message)
* `owarn <id> [message]` - `scpstats.warn` - Warn an offline person by their ID (for example, ID@steam))
* `warnings <id>` - `scpstats.warnings` - View all of a person's warnings
* `deletewarning <id>` - `scpstats.deletewarning` - Delete a specific warning by its Warning ID (you can view the warn ID with the `warnings` command)

## Request Data Removal
If you would like your data to be removed from the site, go to https://scpstats.com/account, and click the `Delete Account` button at the bottom. It will delete all of your account data, as well as all stat data recorded for any accounts linked to your account. If you have any issues, send an email to contact@scpstats.com.
