## Id:1 Command - SendMessage
Id: 1
Command: SendMessage

This command allows a user to send a message to the server

Request:
Additonal Headers:
- None
Payload:
- message (UTF-8 String)

Response:
Additional Headers:
- ResponseCode: 4 bytes, 0 ( fail ) | 1 ( success )
Payload:
- None

