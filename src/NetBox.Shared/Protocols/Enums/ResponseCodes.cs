namespace NetBox.Shared.Protocols.Enums;

/// <summary>
/// This file with contain all the enums related to response codes.
/// </summary>


public enum LoginResponseCode
{
    FAIL = 0,
    SUCCESS = 1,
}

public enum SendMessageResponseCode
{
    FAIL = 0,
    SUCCESS = 1,
}

public enum CreateChannelResponseCode
{
    CHANNEL_CREATED = 0,
    CHANNEL_ALREADY_EXISTS = 1,
}


public enum GetChannelListResponseCode
{
    SUCCESS = 0,
    FAILURE = 1,
}

public enum JoinChannelResponseCode
{
    SUCCESS = 0,
    CHANNEL_NOT_FOUND = 1,
    ALREADY_IN_CHANNEL = 2,
    FAILURE = 3,
}

public enum LeaveChannelResponseCode
{
    SUCCESS = 0,
    NOT_IN_CHANNEL = 1,
    FAILURE = 2,
}