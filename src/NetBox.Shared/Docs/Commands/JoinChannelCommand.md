## Id:5 Command - JoinChannel
Id: 5
Command: JoinChannel

This command allows a user to join an existing channel

Request:
Additonal Headers:
- None
Payload:
- Channel Name (UTF-8 String)

Response:
Additional Headers:
- ResponseCode: 4 bytes, 0 - SUCCESS, 1 - CHANNEL_NOT_FOUND, 2 - ALREADY_IN_CHANNEL, 3 - FAILURE
Payload:
- None

A user can only be part of one channel at a time. If the user is already in a channel, they must leave that channel before joining another.