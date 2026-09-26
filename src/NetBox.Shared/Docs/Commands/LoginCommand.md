## Id:0 Command - Login
Id: 0
Command: Login

This command allows a user to 'login'.

Request:
Additional Headers: 
- None
Payload: 
- username (UTF-8 String)

Response:
Additional Headers:
- ResponseCode: 4 bytes, 0 ( fail ) | 1 ( success )
Payload:
- None
