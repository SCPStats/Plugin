namespace SCPStats
{
    internal enum RequestType
    {
        RoundStart = 0,
        RoundEnd = 1,
        Spawn = 4,
        Pickup = 5,
        Drop = 6,
        Escape = 7,
        Join = 8,
        Leave = 9,
        Use = 10,
        UserData = 11,
        AddWarning = 15,
        GetWarnings = 16,
        DeleteWarnings = 17,
        Win = 18,
        Lose = 19,
        InvalidateBan = 20,
        KillDeath = 21,
        Revive = 22,
        PocketEnter = 23,
        PocketExit = 24,
        RoundEndPlayer = 25
    }
}