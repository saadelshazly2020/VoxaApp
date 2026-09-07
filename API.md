# API Documentation

Complete reference for SignalR Hub methods, events, and services.

## SignalR Hub API

### Base URL
```
/videocallhub
```

### Connection
```typescript
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/videocallhub')
    .build();

await connection.start();
```

---

## Client ? Server Methods

### RegisterUser

Register a user with the system.

**Method**: `RegisterUser`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| userId | string | Yes | Unique identifier for the user |

**Returns**: `void`

**Example**:
```typescript
await connection.invoke('RegisterUser', 'alice');
```

**Server Response Events**:
- `Registered` - Confirmation with userId
- `UserListUpdated` - Updated list of all users
- `Error` - If registration fails

**Error Cases**:
- Empty userId
- Already registered with same connection

---

### CallUser

Initiate a 1-to-1 call with another user.

**Method**: `CallUser`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| targetUserId | string | Yes | User ID to call |
| offer | RTCSessionDescriptionInit | Yes | WebRTC SDP offer |

**Returns**: `void`

**Example**:
```typescript
const offer = await peerConnection.createOffer();
await peerConnection.setLocalDescription(offer);
await connection.invoke('CallUser', 'bob', offer);
```

**Server Response Events**:
- Target user receives `ReceiveOffer` event
- `Error` - If target user not found or caller not registered

**Error Cases**:
- Caller not registered
- Target user not found
- Invalid offer object

---

### AnswerCall

Accept an incoming call.

**Method**: `AnswerCall`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| callerUserId | string | Yes | User ID who initiated the call |
| answer | RTCSessionDescriptionInit | Yes | WebRTC SDP answer |

**Returns**: `void`

**Example**:
```typescript
connection.on('ReceiveOffer', async (fromUserId, offer) => {
    await peerConnection.setRemoteDescription(offer);
    const answer = await peerConnection.createAnswer();
    await peerConnection.setLocalDescription(answer);
    await connection.invoke('AnswerCall', fromUserId, answer);
});
```

**Server Response Events**:
- Caller receives `ReceiveAnswer` event
- `Error` - If caller not found

---

### SendIceCandidate

Exchange ICE candidates for NAT traversal.

**Method**: `SendIceCandidate`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| targetUserId | string | Yes | Recipient user ID |
| candidate | RTCIceCandidateInit | Yes | ICE candidate data |

**Returns**: `void`

**Example**:
```typescript
peerConnection.onicecandidate = async (event) => {
    if (event.candidate) {
        await connection.invoke('SendIceCandidate', 
            targetUserId, 
            event.candidate.toJSON()
        );
    }
};
```

**Server Response Events**:
- Target user receives `ReceiveIceCandidate` event

**Notes**:
- Candidates sent continuously during ICE gathering
- Null candidate indicates end of candidates

---

### CreateRoom

Create a new video call room.

**Method**: `CreateRoom`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| roomId | string | Yes | Unique room identifier |

**Returns**: `string` - The created room ID

**Example**:
```typescript
const roomId = await connection.invoke('CreateRoom', 'meeting-room-1');
```

**Server Response Events**:
- `RoomCreated` - Confirmation with roomId
- `Error` - If creation fails

**Notes**:
- Creates room with creator as first participant
- Room ID must be unique
- Creator automatically joined to room

---

### JoinRoom

Join an existing room.

**Method**: `JoinRoom`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| roomId | string | Yes | Room ID to join |

**Returns**: `void`

**Example**:
```typescript
await connection.invoke('JoinRoom', 'meeting-room-1');
```

**Server Response Events**:
- `RoomJoined` - Confirmation with roomId and participant list
- Existing participants receive `UserJoinedRoom` event
- `Error` - If room not found

**Expected Flow**:
1. Client calls `JoinRoom`
2. Server responds with `RoomJoined(roomId, existingParticipants[])`
3. Client creates offers for all existing participants
4. Existing participants receive `UserJoinedRoom` and create offers

---

### LeaveRoom

Leave the current room.

**Method**: `LeaveRoom`

**Parameters**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| roomId | string | Yes | Room ID to leave |

**Returns**: `void`

**Example**:
```typescript
await connection.invoke('LeaveRoom', 'meeting-room-1');
```

**Server Response Events**:
- `RoomLeft` - Confirmation with roomId
- Other participants receive `UserLeftRoom` event

**Notes**:
- Room deleted automatically if last participant leaves
- Client should close all peer connections

---

## Server ? Client Events

### Registered

User registration confirmed.

**Event**: `Registered`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| userId | string | The registered user ID |

**Example**:
```typescript
connection.on('Registered', (userId) => {
    console.log('Registered as:', userId);
});
```

---

### UserListUpdated

Updated list of all connected users.

**Event**: `UserListUpdated`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| users | User[] | Array of connected users |

**User Object**:
```typescript
interface User {
    userId: string;
    currentRoomId: string | null;
}
```

**Example**:
```typescript
connection.on('UserListUpdated', (users) => {
    users.forEach(user => {
        console.log(`${user.userId} - Room: ${user.currentRoomId || 'None'}`);
    });
});
```

**When Triggered**:
- User registers
- User disconnects
- User joins/leaves room

---

### ReceiveOffer

Incoming call offer.

**Event**: `ReceiveOffer`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| fromUserId | string | Caller's user ID |
| offer | RTCSessionDescriptionInit | WebRTC offer |

**Example**:
```typescript
connection.on('ReceiveOffer', async (fromUserId, offer) => {
    const peerConnection = createPeerConnection(fromUserId);
    await peerConnection.setRemoteDescription(offer);
    const answer = await peerConnection.createAnswer();
    await peerConnection.setLocalDescription(answer);
    await connection.invoke('AnswerCall', fromUserId, answer);
});
```

---

### ReceiveAnswer

Call answer received.

**Event**: `ReceiveAnswer`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| fromUserId | string | Callee's user ID |
| answer | RTCSessionDescriptionInit | WebRTC answer |

**Example**:
```typescript
connection.on('ReceiveAnswer', async (fromUserId, answer) => {
    const peerConnection = getPeerConnection(fromUserId);
    await peerConnection.setRemoteDescription(answer);
});
```

---

### ReceiveIceCandidate

ICE candidate received.

**Event**: `ReceiveIceCandidate`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| fromUserId | string | Sender's user ID |
| candidate | RTCIceCandidateInit | ICE candidate |

**Example**:
```typescript
connection.on('ReceiveIceCandidate', async (fromUserId, candidate) => {
    const peerConnection = getPeerConnection(fromUserId);
    if (candidate) {
        await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
    }
});
```

---

### UserJoinedRoom

New user joined the room.

**Event**: `UserJoinedRoom`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| userId | string | User who joined |
| roomId | string | Room ID |

**Example**:
```typescript
connection.on('UserJoinedRoom', async (userId, roomId) => {
    console.log(`${userId} joined ${roomId}`);
    // Create offer for new user
    await createOfferForUser(userId);
});
```

---

### UserLeftRoom

User left the room.

**Event**: `UserLeftRoom`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| userId | string | User who left |
| roomId | string | Room ID |

**Example**:
```typescript
connection.on('UserLeftRoom', (userId, roomId) => {
    console.log(`${userId} left ${roomId}`);
    closePeerConnection(userId);
});
```

---

### RoomCreated

Room creation confirmed.

**Event**: `RoomCreated`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| roomId | string | Created room ID |

**Example**:
```typescript
connection.on('RoomCreated', (roomId) => {
    console.log('Room created:', roomId);
});
```

---

### RoomJoined

Successfully joined room.

**Event**: `RoomJoined`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| roomId | string | Joined room ID |
| participants | string[] | Existing participant user IDs |

**Example**:
```typescript
connection.on('RoomJoined', async (roomId, participants) => {
    console.log(`Joined ${roomId} with ${participants.length} participants`);
    
    // Create offers for all existing participants
    for (const participantId of participants) {
        await createOfferForUser(participantId);
    }
});
```

---

### RoomLeft

Successfully left room.

**Event**: `RoomLeft`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| roomId | string | Left room ID |

**Example**:
```typescript
connection.on('RoomLeft', (roomId) => {
    console.log('Left room:', roomId);
    closeAllPeerConnections();
});
```

---

### UserDisconnected

User disconnected from server.

**Event**: `UserDisconnected`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| userId | string | Disconnected user ID |

**Example**:
```typescript
connection.on('UserDisconnected', (userId) => {
    console.log(`${userId} disconnected`);
    closePeerConnection(userId);
});
```

---

### Error

Server error occurred.

**Event**: `Error`

**Parameters**:
| Name | Type | Description |
|------|------|-------------|
| message | string | Error description |

**Example**:
```typescript
connection.on('Error', (message) => {
    console.error('Server error:', message);
    showErrorToUser(message);
});
```

**Common Errors**:
- "UserId cannot be empty"
- "You must register first"
- "Target user not found"
- "Room not found"
- "Failed to create room"

---

## TypeScript Service APIs

### SignalRService

Wrapper for SignalR hub connection.

#### Constructor

```typescript
constructor(hubUrl: string = '/videocallhub')
```

#### Methods

**start(): Promise<void>**
- Establishes connection to hub
- Automatically reconnects on failure
- Throws error if max retries exceeded

**stop(): Promise<void>**
- Closes connection gracefully

**isConnected(): boolean**
- Returns true if connected

**getConnectionState(): string**
- Returns: 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'

**on(eventName: string, callback: Function): void**
- Registers event handler

**off(eventName: string, callback?: Function): void**
- Removes event handler

**Hub Methods** (see above for parameters):
- `registerUser(userId: string): Promise<void>`
- `callUser(targetUserId: string, offer: RTCSessionDescriptionInit): Promise<void>`
- `answerCall(callerUserId: string, answer: RTCSessionDescriptionInit): Promise<void>`
- `sendIceCandidate(targetUserId: string, candidate: RTCIceCandidateInit): Promise<void>`
- `createRoom(roomId: string): Promise<string>`
- `joinRoom(roomId: string): Promise<void>`
- `leaveRoom(roomId: string): Promise<void>`

---

### WebRTCService

Manages WebRTC peer connections and media streams.

#### Constructor

```typescript
constructor(signalRService: SignalRService, localUserId: string)
```

#### Methods

**initLocalMedia(constraints?: MediaStreamConstraints): Promise<MediaStream>**

Initializes local camera and microphone.

**Parameters**:
```typescript
constraints?: {
    video?: boolean | MediaTrackConstraints;
    audio?: boolean | MediaTrackConstraints;
}
```

**Default**:
```typescript
{ video: true, audio: true }
```

**Returns**: `Promise<MediaStream>`

**Throws**: Error with user-friendly message if access denied

**Example**:
```typescript
const stream = await webRTCService.initLocalMedia({
    video: {
        width: { ideal: 1280 },
        height: { ideal: 720 }
    },
    audio: {
        echoCancellation: true,
        noiseSuppression: true
    }
});
```

---

**createPeerConnection(userId: string): RTCPeerConnection**

Creates or returns existing peer connection.

**Returns**: RTCPeerConnection instance

**Side Effects**:
- Adds local tracks to connection
- Sets up event handlers (onicecandidate, ontrack, etc.)

---

**createOfferForUser(userId: string): Promise<void>**

Creates and sends offer to specified user.

**Flow**:
1. Creates peer connection
2. Generates offer
3. Sets local description
4. Sends via SignalR

---

**handleOffer(fromUserId: string, offer: RTCSessionDescriptionInit): Promise<void>**

Handles incoming offer.

**Flow**:
1. Creates peer connection
2. Sets remote description
3. Creates answer
4. Sets local description
5. Sends answer via SignalR

---

**handleAnswer(fromUserId: string, answer: RTCSessionDescriptionInit): Promise<void>**

Handles incoming answer.

**Flow**:
1. Gets peer connection
2. Sets remote description

---

**handleIceCandidate(fromUserId: string, candidate: RTCIceCandidateInit): Promise<void>**

Adds ICE candidate to peer connection.

---

**closeConnection(userId: string): void**

Closes peer connection with specific user.

---

**closeAllConnections(): void**

Closes all peer connections.

---

**stopLocalStream(): void**

Stops local media stream (camera/microphone).

---

**toggleAudio(enabled: boolean): Promise<void>**

Enable/disable local audio track.

---

**toggleVideo(enabled: boolean): Promise<void>**

Enable/disable local video track.

---

**isAudioEnabled(): boolean**

Returns true if audio enabled.

---

**isVideoEnabled(): boolean**

Returns true if video enabled.

---

**getLocalStream(): MediaStream | null**

Returns local media stream.

---

**getRemoteStream(userId: string): MediaStream | null**

Returns remote stream for specific user.

---

**getPeerConnection(userId: string): RTCPeerConnection | null**

Returns peer connection for specific user.

---

**getAllPeerConnections(): Record<string, PeerConnectionInfo>**

Returns all peer connections.

---

**cleanup(): void**

Closes all connections and stops local stream.

---

#### Events

**on(eventName: string, callback: Function): void**

Available events:
- `localStreamReady(stream: MediaStream)`
- `remoteStream(userId: string, stream: MediaStream)`
- `connectionClosed(userId: string)`
- `connectionStateChange(userId: string, state: string)`

**Example**:
```typescript
webRTCService.on('remoteStream', (userId, stream) => {
    const videoElement = document.getElementById(`video-${userId}`);
    videoElement.srcObject = stream;
});
```

---

## Data Models

### User

```typescript
interface User {
    userId: string;
    currentRoomId: string | null;
}
```

### UserConnection (Server-side)

```csharp
public class UserConnection
{
    public string UserId { get; set; }
    public string ConnectionId { get; set; }
    public string? CurrentRoomId { get; set; }
    public DateTime ConnectedAt { get; set; }
}
```

### Room (Server-side)

```csharp
public class Room
{
    public string RoomId { get; set; }
    public HashSet<string> ParticipantUserIds { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatorUserId { get; set; }
}
```

### RTCConfiguration

```typescript
interface RTCConfiguration {
    iceServers: RTCIceServer[];
    iceTransportPolicy?: 'all' | 'relay';
}

interface RTCIceServer {
    urls: string | string[];
    username?: string;
    credential?: string;
}
```

**Example**:
```typescript
{
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { 
            urls: 'turn:172.24.32.1:3478',
            username: 'webrtcuser',
            credential: 'StrongPassword123'
        }
    ],
    iceTransportPolicy: 'relay'
}
```

---

## Error Handling

### SignalR Errors

```typescript
try {
    await connection.invoke('CallUser', targetUserId, offer);
} catch (error) {
    console.error('SignalR error:', error);
    // Handle connection issues
}
```

### WebRTC Errors

```typescript
try {
    await webRTCService.initLocalMedia();
} catch (error) {
    if (error.message.includes('Permission denied')) {
        // Show permission instructions
    } else if (error.message.includes('not found')) {
        // No camera/mic available
    } else {
        // Other error
    }
}
```

---

## Rate Limiting

Currently no rate limiting implemented. Consider adding:

```csharp
// Future enhancement
builder.Services.AddRateLimiting(options =>
{
    options.AddFixedWindowLimiter("signalr", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 100;
    });
});
```

---

## Versioning

**Current Version**: 1.0

**Breaking Changes Policy**: 
- Major version for breaking changes
- Minor version for new features
- Patch version for bug fixes

---

## Support

For issues or questions:
- Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- Review [ARCHITECTURE.md](ARCHITECTURE.md)
- Open an issue on GitHub

---

**Last Updated**: 2024
**API Version**: 1.0
