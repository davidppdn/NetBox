using System;
using System.Collections.Generic;
using System.Text;

namespace NetBox.Shared.Protocols.Enums;

public enum Command
{
    LOGIN = 0,
    SEND_MESSAGE = 1,
    CHAT_MESSAGE = 2,
    CREATE_CHANNEL = 3,
    GET_CHANNEL_LIST = 4,
    JOIN_CHANNEL = 5,
    LEAVE_CHANNEL = 6,
}
