## Id:4 Command - GetChannelList
Id: 4
Command: GetChannelList

This command allows a user to retrieve a list of available channels

Request:
Additonal Headers:
- None
Payload:
- None

Response:
Additional Headers:
- ResponseCode: 4 bytes, 0 - SUCCESS, 1 - FAILURE
Payload:
- Channel Count - 4 bytes big endian, number of channels returned
- Per channel:
  - Channel Name Length - 4 bytes big endian, length of the channel name
  - Channel Name - variable length, UTF-8 encoded string representing the channel name

