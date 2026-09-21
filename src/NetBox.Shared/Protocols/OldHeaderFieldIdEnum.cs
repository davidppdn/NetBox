using System;
using System.Collections.Generic;
using System.Text;

namespace NetBox.Shared.Protocols;

/// <summary>
/// An enumeration representing the identifiers for the header fields
/// in the protocol.
/// </summary>
public enum OldHeaderFieldIdEnum
{
    Command = 0,
    Username = 1,
    ResponseCode = 2,
}
