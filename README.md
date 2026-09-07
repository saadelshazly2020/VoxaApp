# Video Chat Application - Production Ready

A full-featured, production-ready video chat application built with **Vue 3 + TypeScript + Tailwind CSS** (frontend) and **ASP.NET Core 9 + SignalR** (backend), featuring WebRTC for peer-to-peer video communication.

## ⚡ Super Quick Start

**Easiest Way**: Double-click `start-dev.bat`

**Or PowerShell**:
```powershell
.\install-deps.ps1
.\start-dev.ps1
```

**Then open**: http://localhost:3000

**📖 New here?** → Read **[GETTING_STARTED.md](GETTING_STARTED.md)** for complete guide!

---

## 📁 New Project Structure

The project has been restructured for better organization:

```
VideoChatingApp.WebRTC/
├── client-app/          # 🎨 Complete Vue 3 frontend
│   ├── src/             # Vue components, services, models
│   ├── package.json
│   └── vite.config.ts
│
├── Core/                # 🔧 .NET business logic
├── Hubs/                # 🔌 SignalR hub
├── Managers/            # 📊 State management
│
├── wwwroot/dist/        # 📦 Built frontend (auto-generated)
│
├── start-dev.ps1        # 🚀 Quick start script
├── start-dev.bat        # 🚀 One-click start
└── Documentation/       # 📚 Comprehensive docs
```

**📖 See**: [NEW_STRUCTURE_README.md](NEW_STRUCTURE_README.md) for detailed structure

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| **[GETTING_STARTED.md](GETTING_STARTED.md)** | Complete beginner guide |
| **[NEW_STRUCTURE_README.md](NEW_STRUCTURE_README.md)** | Project structure explained |
| **[client-app/README.md](client-app/README.md)** | Frontend development guide |
| **[VISUAL_GUIDE.md](VISUAL_GUIDE.md)** | Visual project overview |
| **[DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)** | All docs index |
| **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** | Common issues & solutions |
| **[API.md](API.md)** | SignalR API documentation |
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | System architecture |

---

## ??? Architecture Overview

This application follows **Clean Architecture** and **SOLID principles** with clear separation of concerns:

### Backend Architecture

```
VideoChatingApp.WebRTC/
??? Core/
?   ??? Models/              # Domain entities
?   ?   ??? UserConnection.cs
?   ?   ??? Room.cs
?   ?   ??? SignalingMessage.cs
?   ?   ??? IceCandidateMessage.cs
?   ??? Interfaces/          # Service contracts
?       ??? IUserManager.cs
?       ??? IRoomManager.cs
??? Managers/                # Business logic implementations
?   ??? UserManager.cs       # Thread-safe user management
?   ??? RoomManager.cs       # Thread-safe room management
??? Hubs/                    # SignalR communication layer
?   ??? VideoCallHub.cs      # WebRTC signaling hub
??? Program.cs               # Application startup & DI configuration
```

### Frontend Architecture

```
wwwroot/
??? src/
?   ??? components/          # Vue 3 components
?   ?   ??? VideoChat.ts     # Main application component
?   ?   ??? UserList.ts      # Connected users display
?   ?   ??? VideoGrid.ts     # Video stream layout
?   ??? services/            # Business logic services
?   ?   ??? signalr.service.ts    # SignalR communication
?   ?   ??? webrtc.service.ts     # WebRTC peer management
?   ??? models/              # TypeScript interfaces
?   ?   ??? user.model.ts
?   ?   ??? room.model.ts
?   ?   ??? webrtc.model.ts
?   ??? main.js              # Application entry point
??? index.html               # SPA shell
```

## ? Key Features

### 1. **User Management**
- Thread-safe user registration with `ConcurrentDictionary`
- Real-time user list updates
- Automatic cleanup on disconnection
- Duplicate connection handling

### 2. **1-to-1 Video Calls**
- Direct peer-to-peer video calls
- Full audio and video support
- ICE candidate exchange via SignalR
- Connection state monitoring

### 3. **Multi-User Rooms (Full Mesh)**
- Create and join rooms
- Dynamic participant management
- Automatic peer connection establishment
- Each user maintains connections to all other participants

### 4. **WebRTC Features**
- **STUN Server**: `stun:stun.l.google.com:19302`
- **TURN Server**: Configured for relay (see configuration below)
- Audio/Video toggle controls
- Connection quality monitoring
- Automatic ICE restart on failure

### 5. **Robust Error Handling**
- Automatic reconnection with exponential backoff
- Media device error handling
- Peer connection state monitoring
- User-friendly error messages

## ?? WebRTC Configuration

The application is configured with the following ICE servers:

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

**Note**: Update the TURN server configuration in `wwwroot/src/services/webrtc.service.ts` to match your infrastructure.

## ?? Getting Started

### Prerequisites
- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Modern web browser** with WebRTC support (Chrome, Firefox, Edge, Safari)
- **HTTPS** (required for WebRTC in production)

### Installation & Running

1. **Clone the repository**
```bash
cd VideoChatingApp.WebRTC
```

2. **Restore dependencies**
```bash
dotnet restore
```

3. **Run the application**
```bash
dotnet run
```

4. **Access the application**
- Open your browser to: `https://localhost:5001` (or the port shown in console)
- Open multiple tabs/windows to test with different users

### Development Mode

```bash
dotnet watch run
```

This enables hot reload for both backend and frontend changes.

## ?? SignalR Hub Methods

### Client ? Server

| Method | Parameters | Description |
|--------|-----------|-------------|
| `RegisterUser` | `userId: string` | Register user and get user list |
| `CallUser` | `targetUserId: string, offer: object` | Initiate 1-to-1 call |
| `AnswerCall` | `callerUserId: string, answer: object` | Accept incoming call |
| `SendIceCandidate` | `targetUserId: string, candidate: object` | Exchange ICE candidates |
| `CreateRoom` | `roomId: string` | Create a new room |
| `JoinRoom` | `roomId: string` | Join existing room |
| `LeaveRoom` | `roomId: string` | Leave current room |

### Server ? Client

| Event | Parameters | Description |
|-------|-----------|-------------|
| `Registered` | `userId: string` | Confirm registration |
| `UserListUpdated` | `users: User[]` | Broadcast updated user list |
| `ReceiveOffer` | `fromUserId: string, offer: object` | Receive call offer |
| `ReceiveAnswer` | `fromUserId: string, answer: object` | Receive call answer |
| `ReceiveIceCandidate` | `fromUserId: string, candidate: object` | Receive ICE candidate |
| `UserJoinedRoom` | `userId: string, roomId: string` | New user joined room |
| `UserLeftRoom` | `userId: string, roomId: string` | User left room |
| `RoomCreated` | `roomId: string` | Room created successfully |
| `RoomJoined` | `roomId: string, participants: string[]` | Joined room with participant list |
| `RoomLeft` | `roomId: string` | Left room successfully |
| `UserDisconnected` | `userId: string` | User disconnected |
| `Error` | `message: string` | Error occurred |

## ?? WebRTC Signaling Flow

### 1-to-1 Call Negotiation

```
User A (Caller)                SignalR Hub                User B (Callee)
      |                              |                            |
      |--RegisterUser--------------->|                            |
      |                              |<--------RegisterUser-------|
      |<--UserListUpdated------------|                            |
      |                              |--UserListUpdated---------->|
      |                              |                            |
      |--CallUser(B, offer)--------->|                            |
      |                              |--ReceiveOffer(A, offer)--->|
      |                              |                            |
      |                              |<--AnswerCall(A, answer)----|
      |<--ReceiveAnswer(B, answer)---|                            |
      |                              |                            |
      |--SendIceCandidate(B)-------->|                            |
      |                              |--ReceiveIceCandidate(A)--->|
      |                              |                            |
      |                              |<--SendIceCandidate(A)------|
      |<--ReceiveIceCandidate(B)-----|                            |
      |                              |                            |
      |<=========== WebRTC Media Connection Established =========>|
```

### Multi-User Room Flow

```
User A (Creator)              SignalR Hub              User B              User C
      |                            |                      |                   |
      |--CreateRoom("room1")------>|                      |                   |
      |<--RoomCreated--------------|                      |                   |
      |                            |                      |                   |
      |                            |<--JoinRoom("room1")--|                   |
      |<--UserJoinedRoom(B)--------|                      |                   |
      |                            |--RoomJoined--------->|                   |
      |                            |                      |                   |
      |--CreateOffer(B)----------->|                      |                   |
      |                            |--ReceiveOffer(A)---->|                   |
      |                            |<--AnswerCall(A)------|                   |
      |<--ReceiveAnswer(B)---------|                      |                   |
      |<======= Media A-B ========>|                      |                   |
      |                            |                      |                   |
      |                            |                      |<--JoinRoom("room1")
      |<--UserJoinedRoom(C)--------|--UserJoinedRoom(C)-->|                   |
      |                            |--RoomJoined--------->|------------------>|
      |                            |                      |                   |
      |--CreateOffer(C)-------------------------------------------------------->|
      |                            |<--CreateOffer(C)-----|                   |
      |                            |                      |                   |
      |<======= Full Mesh: A-B, A-C, B-C Media Connections ==================>|
```

## ?? Common WebRTC Issues & Solutions

### Issue 1: Camera/Microphone Access Denied
**Problem**: User denies permissions or device not found.

**Solution**:
```typescript
// Implemented in webrtc.service.ts
catch (error) {
    if (error.name === 'NotAllowedError') {
        throw new Error('Permission denied. Please allow camera and microphone access');
    }
    if (error.name === 'NotFoundError') {
        throw new Error('No camera or microphone found');
    }
}
```

### Issue 2: Video Autoplay Blocked
**Problem**: Browsers block autoplay without user interaction.

**Solution**:
```typescript
// Implemented in VideoGrid.ts
video.play().catch(error => {
    console.warn('Autoplay failed:', error);
    video.onclick = () => {
        video.play();
        video.onclick = null;
    };
});
```

### Issue 3: ICE Connection Failed
**Problem**: Peer connection fails due to NAT/firewall.

**Solution**: Configure TURN server for relay:
```typescript
iceTransportPolicy: 'relay' // Forces traffic through TURN
```

### Issue 4: Connection Drops
**Problem**: Network interruption or connection state change.

**Solution**:
```typescript
// Automatic ICE restart implemented
peerConnection.onconnectionstatechange = () => {
    if (state === 'failed') {
        peerConnection.restartIce();
    }
};
```

### Issue 5: SignalR Disconnection
**Problem**: Server restart or network issue.

**Solution**: Automatic reconnection with exponential backoff:
```typescript
.withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
    }
})
```

## ?? Security Considerations

### Current Implementation (Development)
- CORS allows all origins
- No authentication/authorization
- SignalR connections anonymous

### Production Recommendations

1. **Enable Authentication**
```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

app.UseAuthentication();
app.UseAuthorization();

// VideoCallHub.cs
[Authorize]
public class VideoCallHub : Hub
{
    // Use Context.User.Identity.Name for userId
}
```

2. **Configure CORS**
```csharp
options.AddPolicy("Production", policy =>
{
    policy.WithOrigins("https://yourdomain.com")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
```

3. **Use HTTPS**
```bash
dotnet dev-certs https --trust
```

4. **Secure TURN Server**
- Use time-limited credentials
- Implement TURN REST API
- Monitor usage and rate limit

## ?? Performance & Scalability

### Current Architecture
- **In-Memory State**: Uses `ConcurrentDictionary`
- **Single Server**: All users on one instance
- **Full Mesh**: N×(N-1)/2 connections in a room

### Scaling Strategies

#### 1. **Redis Backplane** (Multi-Server)
```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379");
```

#### 2. **SFU Architecture** (Better for Large Rooms)
Replace full mesh with Selective Forwarding Unit:
- Use **mediasoup** or **Janus**
- Each client sends 1 stream, receives N streams
- Dramatically reduces bandwidth

#### 3. **Load Balancing**
- Enable sticky sessions
- Share user state via Redis
- Use Azure SignalR Service

## ?? Testing

### Manual Testing Steps

1. **Single User Test**
   - Open browser, register as "Alice"
   - Verify camera/microphone access
   - Check local video feed

2. **1-to-1 Call Test**
   - Open second tab, register as "Bob"
   - Alice calls Bob
   - Verify bidirectional audio/video

3. **Multi-User Room Test**
   - Open three tabs: Alice, Bob, Charlie
   - Alice creates room "test-room"
   - Bob and Charlie join "test-room"
   - Verify all three see each other

4. **Disconnection Test**
   - Establish connections
   - Close one tab
   - Verify others receive disconnection event

### Browser Compatibility
- ? Chrome 90+
- ? Firefox 88+
- ? Edge 90+
- ? Safari 15+
- ? Mobile browsers (iOS Safari, Chrome Mobile)

## ?? Project Structure Details

### Backend

**Models**: Pure data structures, no logic
- `UserConnection`: Represents a connected user
- `Room`: Represents a video call room
- `SignalingMessage`: WebRTC signaling data
- `IceCandidateMessage`: ICE candidate structure

**Interfaces**: Service contracts for dependency injection
- `IUserManager`: User CRUD operations
- `IRoomManager`: Room CRUD operations

**Managers**: Business logic with thread safety
- `UserManager`: Thread-safe user state management
- `RoomManager`: Thread-safe room state management

**Hubs**: SignalR communication layer
- `VideoCallHub`: Orchestrates signaling between clients

### Frontend

**Components**: Vue 3 composition API
- `VideoChat`: Main application component
- `UserList`: Display and interact with users
- `VideoGrid`: Display video streams in grid

**Services**: Business logic separation
- `SignalRService`: WebSocket communication
- `WebRTCService`: Peer connection management

**Models**: TypeScript type safety
- Type definitions for users, rooms, WebRTC data

## ??? Extending the Application

### Add Screen Sharing

```typescript
// In webrtc.service.ts
public async startScreenShare(): Promise<MediaStream> {
    const screenStream = await navigator.mediaDevices.getDisplayMedia({
        video: true,
        audio: false
    });
    
    // Replace video track in all peer connections
    const videoTrack = screenStream.getVideoTracks()[0];
    Object.values(this.peerConnections).forEach(peerInfo => {
        const sender = peerInfo.connection.getSenders()
            .find(s => s.track?.kind === 'video');
        if (sender) {
            sender.replaceTrack(videoTrack);
        }
    });
    
    return screenStream;
}
```

### Add Chat Messaging

```csharp
// In VideoCallHub.cs
public async Task SendMessage(string roomId, string message)
{
    var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
    var participants = _roomManager.GetRoomParticipants(roomId);
    
    foreach (var participantId in participants)
    {
        var connectionId = _userManager.GetConnectionIdByUserId(participantId);
        if (connectionId != null)
        {
            await Clients.Client(connectionId)
                .SendAsync("ReceiveMessage", user.UserId, message);
        }
    }
}
```

### Add Recording

Integrate with backend recording service:
- Capture MediaStream with MediaRecorder API
- Send chunks to backend via SignalR or HTTP
- Backend stitches and stores video files

## ?? License

This project is provided as-is for educational and commercial use.

## ?? Contributing

Contributions welcome! Areas for improvement:
- Add unit tests
- Implement authentication
- Add recording feature
- Improve UI/UX
- Add chat messaging
- Implement screen sharing

## ?? Support

For issues and questions:
- Check "Common WebRTC Issues" section above
- Review browser console for errors
- Verify TURN server configuration
- Ensure HTTPS in production

---

**Built with ?? using ASP.NET Core 9, SignalR, Vue.js 3, and WebRTC**

