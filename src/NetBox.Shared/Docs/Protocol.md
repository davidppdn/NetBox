This document defines the general format of the created protocol, explaining the decisions made.

General Shape:

[4 bytes] - Message Length ( bytes )
[N bytes] - Message

Message contains the following:
[4 bytes] - Header Length ( bytes )
[M bytes] - Header
[4 bytes] - Payload Length ( bytes )
[N bytes] - Payload

Header contains the following:
[4 bytes] - HeaderField Count
[X bytes] - HeaderField

HeaderField contains the following:
[4 bytes] - HeaderField Length ( bytes )
[4 bytes] - HeaderFieldId
[Y bytes] - HeaderField Value

Note: All lengths are a count of the number of bytes, as different encoding format contain variable byte lengths for
different characters.

Decisions:
This format is a very free format, where the header can be used to shove any additionally needed fields. This allows
for a lot of freedom in how requests and responses get constructed.

The lengths are all 4 bytes long, which allows for deterministic parsing of the message, while allowing for variable length values.
All lengths are the count of the number of bytes, because different encoding formats contain variable byte lengths. 
Additionally, 4 bytes = 32^2, which is plenty for the lengths.

The headerFieldId is also 4 bytes, which provides many values to set different header values. This makes it easier for parsers
to correspond a value to a header type, without having to rely on string conversions and comparisons, which is more mistake prone.