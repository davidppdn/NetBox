# NetBox Roadmap

## Goal

Build a small Discord-like client/server application where multiple users can:

* Connect to a server
* Authenticate
* Exchange messages
* Join channels
* Receive real-time events
* Persist data
* Communicate securely
* Eventually run as a deployable distributed application

The project is primarily a **networking and systems-learning project**.

The goal is not to build every feature as quickly as possible. Each feature should introduce a real problem that leads naturally into the next networking or systems concept.

---

# Phase 1 — Core TCP & Protocol

**Status: Complete**

### Networking

* [x] TCP server
* [x] Multiple simultaneous clients
* [x] Per-client connection handling
* [x] Connection lifecycle
* [x] Graceful disconnects
* [x] Cancellation
* [x] Handling abrupt client disconnects

### Protocol

* [x] TCP message framing
* [x] Big-endian integer encoding
* [x] Command IDs
* [x] Headers
* [x] Payloads
* [x] Serialization
* [x] Deserialization
* [x] Partial TCP reads
* [x] Multiple messages in one TCP read
* [x] `MessageParser`

### Architecture

* [x] `ClientSession`
* [x] Generic `Message`
* [x] Request/response objects
* [x] Shared protocol layer

Current message structure:

```text
Message
├── Length
├── Command
├── Header
└── Payload
```

---

# Phase 2 — Basic Messaging

**Status: In Progress**

The goal of this phase is to establish complete request/response communication.

## 2.1 LOGIN

* [x] `LoginRequest`
* [x] `LoginResponse`
* [x] Client sends login request
* [x] Server processes login
* [x] Server updates `ClientSession`
* [x] Server sends login response
* [x] Client processes login response

Flow:

```text
Client
  │
  │ LOGIN
  ▼
Server
  │
  │ LOGIN response
  ▼
Client
```

---

## 2.2 SEND_MESSAGE

* [x] `SendMessageRequest`
* [x] `SendMessageResponse`
* [ ] Client sends `SEND_MESSAGE`
* [ ] Server receives `SEND_MESSAGE`
* [ ] Server validates request
* [ ] Server sends `SendMessageResponse`
* [ ] Client processes response

Initially, **do not broadcast the message**.

The first goal is simply to establish a complete request/response cycle.

Flow:

```text
Client
  │
  │ SEND_MESSAGE("hello")
  ▼
Server
  │
  │ SUCCESS
  ▼
Client
```

---

# Phase 3 — Real-Time Chat

## 3.1 CHAT_MESSAGE

Introduce server-initiated messages.

```text
Client A
  │
  │ SEND_MESSAGE("hello")
  ▼
Server
  │
  │ CHAT_MESSAGE
  ▼
Client B
```

* [ ] `ChatMessage`
* [ ] Server creates `CHAT_MESSAGE`
* [ ] Client handles unsolicited messages
* [ ] Define whether sender receives their own message

Important concept:

```text
Request
Response
Event
```

`CHAT_MESSAGE` is an event, not a response to `SEND_MESSAGE`.

---

## 3.2 Broadcasting

* [ ] Broadcast messages to connected clients
* [ ] Decide whether sender receives their own message
* [ ] Handle disconnected clients during broadcast
* [ ] Consider sequential vs concurrent sending
* [ ] Consider slow clients

---

# Phase 4 — Users & Sessions

Expand `ClientSession` as actual application requirements appear.

Current:

```text
ClientSession
├── TcpClient
└── Username
```

Potential future state:

```text
ClientSession
├── Connection
├── Username
├── Authentication state
└── Joined channels
```

### Authentication rules

* [ ] Define unauthenticated state
* [ ] Define authenticated state
* [ ] Restrict commands that require authentication
* [ ] Reject unauthenticated `SEND_MESSAGE`
* [ ] Define login failure behaviour

Example:

```text
Not logged in
      │
      │ LOGIN
      ▼
Authenticated
```

---

# Phase 5 — Channels

Move from one global chat room to multiple channels.

## Commands

* [ ] `JOIN_CHANNEL`
* [ ] `LEAVE_CHANNEL`

## Channel model

Potentially:

```text
Channel
├── Name
└── Connected members
```

Example:

```text
Alice → JOIN_CHANNEL("general")
Bob   → JOIN_CHANNEL("general")

Alice → SEND_MESSAGE("hello")

             ↓

        #general members
             ↓

           Bob
```

---

# Phase 6 — Presence

Introduce server-generated events representing changes to connected users.

## Events

* [ ] `USER_JOINED`
* [ ] `USER_LEFT`

Example:

```text
Bob connects

Server
 ├──→ Alice: USER_JOINED(Bob)
 └──→ Charlie: USER_JOINED(Bob)
```

This further develops the distinction between:

* Requests
* Responses
* Server events

---

# Phase 7 — Client Architecture

Refactor the client when the existing structure starts becoming painful.

Potential structure:

```text
NetBox.Client
├── Networking
│   └── NetBoxConnection
├── Protocol
│   └── Message handling
├── Session
│   └── Current user
└── UI
    └── Console interaction
```

Potential API:

```text
client.Login(...)
client.SendMessage(...)
client.JoinChannel(...)
```

Do not build this abstraction prematurely.

Let repetition and complexity justify it.

---

# Phase 8 — Concurrency

Explore the concurrency problems that emerge from a real-time application.

The client will eventually have:

```text
Client
 ├── Sending messages
 └── Receiving messages
```

The server will have:

```text
Server
 ├── Accepting connections
 ├── Reading clients
 ├── Processing messages
 └── Broadcasting messages
```

## Topics

* [ ] Concurrent writes
* [ ] Per-client outgoing queues
* [ ] Producer/consumer patterns
* [ ] Backpressure
* [ ] Slow clients
* [ ] Bounded queues
* [ ] Message ordering
* [ ] Disconnect handling

Important question:

> What happens if the server wants to send two messages to the same client at the same time?

---

# Phase 9 — Persistence

Introduce a database once the application actually needs persistent state.

Potential database:

**PostgreSQL**

Potential tables:

```text
Users
├── Id
├── Username
└── PasswordHash

Channels
├── Id
└── Name

Messages
├── Id
├── UserId
├── ChannelId
├── Content
└── CreatedAt
```

Architecture becomes:

```text
TCP
 ↓
Protocol
 ↓
Application logic
 ↓
Persistence
 ↓
PostgreSQL
```

Topics:

* [ ] Database integration
* [ ] User persistence
* [ ] Channel persistence
* [ ] Message persistence
* [ ] Database relationships
* [ ] Transactions
* [ ] Connection management

---

# Phase 10 — Security

Once the basic system works, start deliberately testing and securing it.

## Authentication

* [ ] Password hashing
* [ ] Password salts
* [ ] Authentication tokens/sessions
* [ ] Session expiration

## Transport security

Current:

```text
TCP
 ↓
NetBox protocol
```

Eventually:

```text
TCP
 ↓
TLS
 ↓
NetBox protocol
```

Learn what TLS actually provides rather than treating it as a generic security layer.

## Protocol security

Test malformed input:

* [ ] Invalid command IDs
* [ ] Negative lengths
* [ ] Extremely large lengths
* [ ] Malformed headers
* [ ] Invalid UTF-8
* [ ] Unexpected commands
* [ ] Duplicate headers
* [ ] Messages before authentication
* [ ] Invalid response codes

---

# Phase 11 — Performance

Only optimize after the application works.

Investigate:

* [ ] Unnecessary allocations
* [ ] `Span<T>`
* [ ] `ReadOnlySpan<T>`
* [ ] `Memory<T>`
* [ ] `ArrayPool<T>`
* [ ] Socket buffers
* [ ] Concurrent collections
* [ ] Batching
* [ ] Throughput
* [ ] Latency

Current known optimization area:

```text
MessageParser
```

The parser currently performs some unnecessary copies such as:

```text
ToArray()
```

These can be revisited once the protocol is stable.

---

# Phase 12 — Observability

Make the server easier to understand and operate.

## Logging

* [ ] Structured logging
* [ ] Connection events
* [ ] Authentication events
* [ ] Message events
* [ ] Error logging

## Metrics

Potential metrics:

```text
Connected clients
Messages/sec
Active channels
Average message latency
Total messages
Connection count
Error count
```

Eventually investigate:

* [ ] Metrics
* [ ] Distributed tracing
* [ ] OpenTelemetry

---

# Phase 13 — Deployment

Move NetBox from a local development project toward an actual deployable application.

## Containers

* [ ] Dockerize server
* [ ] Dockerize client if useful
* [ ] Run PostgreSQL in Docker
* [ ] Configure services through environment variables
* [ ] Container networking

Architecture:

```text
                    Internet
                       │
                       ▼
                 ┌──────────┐
                 │ NetBox   │
                 │ Server   │
                 └────┬─────┘
                      │
                ┌─────▼─────┐
                │ PostgreSQL│
                └───────────┘
```

## Deployment

* [ ] Configuration management
* [ ] Health checks
* [ ] TLS certificates
* [ ] Server deployment
* [ ] Database deployment
* [ ] Monitoring

---

# Phase 14 — Distributed Systems

Only after NetBox has developed actual scaling problems.

Potential topics:

* [ ] Multiple server instances
* [ ] Load balancing
* [ ] Shared state
* [ ] Distributed messaging
* [ ] Pub/sub
* [ ] Redis
* [ ] Message brokers
* [ ] Server-to-server communication
* [ ] Distributed authentication/session state

Architecture might eventually become:

```text
                    Clients
                       │
                       ▼
                ┌────────────┐
                │Load Balancer│
                └─────┬──────┘
                      │
             ┌────────┴────────┐
             ▼                 ▼
       NetBox Server      NetBox Server
             │                 │
             └────────┬────────┘
                      ▼
                 Message Bus
                      │
             ┌────────┴────────┐
             ▼                 ▼
         PostgreSQL          Redis
```

---

# Phase 15 — Kubernetes

Kubernetes should come **after** the application has developed problems that Kubernetes can actually solve.

Potential topics:

* [ ] Kubernetes fundamentals
* [ ] Deployments
* [ ] Services
* [ ] ConfigMaps
* [ ] Secrets
* [ ] Health probes
* [ ] Horizontal scaling
* [ ] Service discovery
* [ ] Rolling deployments
* [ ] Kubernetes networking

The goal is to understand:

> **Why do I need this?**

rather than simply learning Kubernetes commands.

---

# Guiding Principles

## 1. Let problems drive abstractions

Don't create abstractions because they *might* be useful.

Wait until repetition or complexity creates a real problem.

---

## 2. Don't optimize prematurely

First make it:

```text
Correct
  ↓
Understandable
  ↓
Maintainable
  ↓
Efficient
```

---

## 3. Each feature should teach something

Whenever possible, a new feature should introduce a new concept.

For example:

```text
TCP
 ↓
Message framing
 ↓
Protocol design
 ↓
Request/response
 ↓
Events
 ↓
Broadcasting
 ↓
Concurrency
 ↓
Persistence
 ↓
Security
 ↓
Performance
 ↓
Deployment
 ↓
Distributed systems
```

---

# Current Position

```text
Phase 1 — Core TCP & Protocol       COMPLETE
Phase 2 — Basic Messaging           IN PROGRESS
Phase 3 — Real-Time Chat            NEXT
Phase 4 — Users & Sessions
Phase 5 — Channels
Phase 6 — Presence
Phase 7 — Client Architecture
Phase 8 — Concurrency
Phase 9 — Persistence
Phase 10 — Security
Phase 11 — Performance
Phase 12 — Observability
Phase 13 — Deployment
Phase 14 — Distributed Systems
Phase 15 — Kubernetes
```

## Immediate Next Steps

1. Finish `SEND_MESSAGE` client integration.
2. Finish `SEND_MESSAGE` server handling.
3. Return `SendMessageResponse`.
4. Verify the complete request/response cycle.
5. Implement `CHAT_MESSAGE`.
6. Implement server broadcasting.
7. Test multiple clients communicating with each other.
