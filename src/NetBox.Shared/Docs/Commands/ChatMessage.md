## Id:2 Command - ChatMessage
Id: 2
Command: ChatMessage

This command allows a user to send a chat message to the server, and the server broadcasts the message to all other connected clients.

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

