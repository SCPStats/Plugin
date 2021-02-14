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
        Death = 12,
        Kill = 13,
        AddWarning = 15,
        GetWarnings = 16,
        DeleteWarnings = 17
    }
}