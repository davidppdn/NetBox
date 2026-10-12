## Id:6 Command - LeaveChannel
Id: 6
Command: LeaveChannel

This command allows a user to leave their current channel

Request:
Additonal Headers:
- None
Payload:
- None

Response:
Additional Headers:
- ResponseCode: 4 bytes, 0 - SUCCESS, 1 - NOT_IN_CHANNEL, 2 - FAILURE
Payload:
- None

A user can only be part of one channel at a time. If the user is not in a channel, this command will fail.