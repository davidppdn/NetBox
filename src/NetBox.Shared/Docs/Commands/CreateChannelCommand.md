## Id:3 Command - CreateChannel
Id: 3
Command: CreateChannel

This command allows a user to create a new channel

Request:
Additonal Headers:
- None
Payload:
- Channel Name (UTF-8 String)

Response:
Additional Headers:
- ResponseCode: 4 bytes, 0 - CHANNEL_CREATED, 1 - CHANNEL_ALREADY_EXISTS
Payload:
- None

