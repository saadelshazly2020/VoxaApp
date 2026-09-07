# Architecture Documentation

## System Architecture

### High-Level Overview

```
???????????????????????????????????????????????????????????????
?                        Client Browser                        ?
?  ??????????????  ????????????????  ????????????????????    ?
?  ?  Vue.js 3  ?  ?   SignalR    ?  ?  WebRTC Client   ?    ?
?  ? Components ????   Client     ?  ?   (Browser API)  ?    ?
?  ??????????????  ????????????????  ????????????????????    ?
???????????????????????????????????????????????????????????????
                            ?
                            ? WebSocket (SignalR)
                            ?
???????????????????????????????????????????????????????????????
?                    ASP.NET Core 9 Server                     ?
?  ??????????????  ????????????????  ????????????????????    ?
?  ?  SignalR   ?  ?   Managers   ?  ?      Models      ?    ?
?  ?    Hub     ????  (Business)  ????  (Domain DTOs)   ?    ?
?  ??????????????  ????????????????  ????????????????????    ?
???????????????????????????????????????????????????????????????
                            ?
                            ? Direct P2P Connection
                            ?
???????????????????????????????????????????????????????????????
?                    WebRTC Peer Connection                    ?
?                  (Audio/Video Streaming)                     ?
???????????????????????????????????????????????????????????????
```

## Clean Architecture Layers

### 1. Core Layer (Domain)
**Purpose**: Contains business entities and interfaces. No dependencies on external libraries.

```
Core/
??? Models/              # Domain entities (POCOs)
?   ??? UserConnection   # User state representation
?   ??? Room             # Room entity
?   ??? Messages         # Signaling message structures
??? Interfaces/          # Service contracts
    ??? IUserManager     # User management abstraction
    ??? IRoomManager     # Room management abstraction
```

**Principles Applied**:
- ? **Single Responsibility**: Each model has one reason to change
- ? **Dependency Inversion**: Interfaces define contracts
- ? **No External Dependencies**: Pure C# code

### 2. Application Layer (Managers)
**Purpose**: Implements business logic defined by Core interfaces.

```
Managers/
??? UserManager          # Thread-safe user state management
??? RoomManager          # Thread-safe room state management
```

**Key Features**:
- `ConcurrentDictionary` for thread safety
- Comprehensive error handling and logging
- Idempotent operations where possible

**Principles Applied**:
- ? **Open/Closed**: Extensible without modification
- ? **Liskov Substitution**: Implementations are interchangeable
- ? **Interface Segregation**: Focused, cohesive interfaces

### 3. Infrastructure Layer (Hubs)
**Purpose**: External communication (SignalR).

```
Hubs/
??? VideoCallHub         # SignalR communication hub
```

**Responsibilities**:
- Receive client messages
- Validate input
- Delegate to managers
- Send responses

**Principles Applied**:
- ? **Dependency Inversion**: Depends on IUserManager, IRoomManager abstractions
- ? **Single Responsibility**: Only handles communication

### 4. Presentation Layer (Program.cs)
**Purpose**: Application startup, DI configuration, middleware pipeline.

**Configuration**:
```csharp
Services Registration (DI Container):
??? SignalR
??? CORS Policy
??? Logging
??? IUserManager ? UserManager (Singleton)
??? IRoomManager ? RoomManager (Singleton)

Middleware Pipeline:
??? CORS
??? Static Files
??? SignalR Hub Endpoint
??? Default Route
```

## SOLID Principles Implementation

### Single Responsibility Principle (SRP)
? **Applied**:
- `UserManager`: Only manages users
- `RoomManager`: Only manages rooms
- `VideoCallHub`: Only handles communication
- Each model represents one concept

### Open/Closed Principle (OCP)
? **Applied**:
- New features can be added via new managers
- Existing code doesn't need modification
- Example: Add `IRecordingManager` without touching existing managers

### Liskov Substitution Principle (LSP)
? **Applied**:
- Any `IUserManager` implementation can replace `UserManager`
- Any `IRoomManager` implementation can replace `RoomManager`
- Mock implementations can be used for testing

### Interface Segregation Principle (ISP)
? **Applied**:
- `IUserManager` has focused user operations
- `IRoomManager` has focused room operations
- No client forced to depend on unused methods

### Dependency Inversion Principle (DIP)
? **Applied**:
- `VideoCallHub` depends on `IUserManager`, `IRoomManager` (abstractions)
- Concrete implementations injected via DI
- High-level modules don't depend on low-level modules

## Frontend Architecture

### Component Hierarchy

```
App Root (VideoChat)
??? Header (Status Display)
??? Sidebar
?   ??? Registration Form
?   ??? Room Controls
?   ??? UserList Component
?       ??? User Items (with actions)
??? Content Area
    ??? VideoGrid Component
    ?   ??? Local Video
    ?   ??? Remote Videos (dynamic)
    ??? Control Panel
        ??? Audio Toggle
        ??? Video Toggle
        ??? End Call Button
```

### Service Layer Pattern

```
Component (UI Logic)
       ?
       ?
Service Layer (Business Logic)
??? SignalRService     # Communication abstraction
?   ??? Connection management
?   ??? Event handling
?   ??? Hub method invocation
??? WebRTCService      # WebRTC abstraction
    ??? Media device management
    ??? Peer connection lifecycle
    ??? Stream handling
```

**Benefits**:
- ? **Testability**: Services can be mocked
- ? **Reusability**: Services can be used by multiple components
- ? **Separation of Concerns**: UI logic separated from business logic
- ? **Type Safety**: Full TypeScript typing

## Data Flow

### User Registration Flow

```
????????????         ??????????????????         ????????????????
?  Client  ???????????  VideoCallHub  ??????????? UserManager  ?
?          ? Register?                ?  Add    ?              ?
?          ?         ?                ?  User   ?              ?
?          ???????????                ???????????              ?
?          ? Success ?                ?         ?              ?
????????????         ??????????????????         ????????????????
     ?
     ? Broadcast
     ?
All Connected Clients
(UserListUpdated)
```

### Call Establishment Flow

```
Caller                  Hub                   Callee
  ?                      ?                      ?
  ???CreateOffer()????????                      ?
  ?                      ???ReceiveOffer()???????
  ?                      ?                      ?
  ?                      ????AnswerCall()????????
  ????ReceiveAnswer()?????                      ?
  ?                      ?                      ?
  ???SendIceCandidate()???                      ?
  ?                      ???ReceiveIceCandidate??
  ?                      ?                      ?
  ????SendIceCandidate()?????????????????????????
  ?                      ?                      ?
  ???????????????????????????????????????????????
          WebRTC P2P Connection Established
```

### Room Join Flow

```
New User              Hub                Existing Peers
  ?                    ?                      ?
  ???JoinRoom()?????????                      ?
  ?                    ???GetParticipants()????
  ?                    ?                      ?
  ????RoomJoined()??????                      ?
  ?   (peer list)      ?                      ?
  ?                    ?                      ?
  ?                    ???UserJoinedRoom()?????
  ?                    ?                      ?
  ?                    ?                      ?
  ????????????????????????CreateOffer()????????
  ?                    ?                      ?
  ???AnswerCall()???????????????????????????  ?
  ?                    ?                      ?
  ?????????????????????????????????????????????
    WebRTC connections established with all peers
```

## Concurrency & Thread Safety

### Thread-Safe Operations

**Problem**: Multiple clients connecting/disconnecting simultaneously

**Solution**: `ConcurrentDictionary<TKey, TValue>`

```csharp
// UserManager.cs
private readonly ConcurrentDictionary<string, UserConnection> _usersByConnectionId;
private readonly ConcurrentDictionary<string, string> _connectionIdsByUserId;

// Atomic operations
_usersByConnectionId[connectionId] = userConnection;  // Thread-safe
_usersByConnectionId.TryRemove(connectionId, out _);  // Thread-safe
```

**Benefits**:
- No explicit locking needed
- High concurrent read performance
- Safe for multiple simultaneous updates

### SignalR Concurrency Model

- Each hub method invocation runs on a separate thread
- Hub instances are created per-connection (transient lifetime)
- Shared state (managers) must be thread-safe
- Our singleton managers use `ConcurrentDictionary` for safety

## WebRTC Architecture

### ICE (Interactive Connectivity Establishment)

```
???????????????????????????????????????????????????????????
?                    ICE Candidate Gathering              ?
???????????????????????????????????????????????????????????
?  1. Host Candidates (Local IP)                          ?
?     ?? Direct connection if possible                    ?
?                                                          ?
?  2. Server Reflexive Candidates (STUN)                  ?
?     ?? Public IP discovered via STUN server             ?
?                                                          ?
?  3. Relay Candidates (TURN)                             ?
?     ?? Traffic relayed through TURN server              ?
?        (Used when direct/NAT traversal fails)           ?
???????????????????????????????????????????????????????????
```

**Configuration Strategy**:
```typescript
iceTransportPolicy: 'relay'  // Force TURN for all connections
```

**Why?**
- Guaranteed connectivity in corporate networks
- Consistent behavior across NAT types
- Production-ready out of the box

### Full Mesh vs SFU

**Current: Full Mesh Topology**
```
        User A
       /  |  \
      /   |   \
     /    |    \
  User B?????User C
     \    |    /
      \   |   /
       \  |  /
        User D
```

**Connections**: N × (N-1) / 2
- 3 users = 3 connections
- 5 users = 10 connections
- 10 users = 45 connections

**Pros**:
- Low latency
- No media server needed
- Simple implementation

**Cons**:
- Bandwidth scales poorly (each user uploads N-1 streams)
- CPU intensive for encoding multiple streams

**Future: SFU (Selective Forwarding Unit)**
```
  User A ??????
              ?
  User B ??????
              ???? SFU Server ???? To all users
  User C ??????
              ?
  User D ??????
```

**Connections**: N (one per user to SFU)

**Pros**:
- Scales to hundreds of users
- Each user uploads one stream
- Lower bandwidth per client

**Cons**:
- Requires media server (mediasoup, Janus)
- Higher latency
- Server infrastructure needed

## Security Architecture

### Current State (Development)

```
Client ??????????????????????? Server
        (No authentication)
```

**Issues**:
- No identity verification
- Open CORS policy
- Anonymous connections

### Recommended Production Architecture

```
Client ????????????? API Gateway ????????? SignalR Hub
       JWT Token                 Validate         ?
       in query                   Token           ?
       string                                     ?
                                                  ?
                         ??????????????????????????
                         ?
                         ?
                  IUserManager
                  (Stores authenticated userId)
```

**Implementation**:
```csharp
[Authorize]
public class VideoCallHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        // Use authenticated identity, not client-provided userId
    }
}
```

## Extensibility Points

### 1. Add Redis for Multi-Server Scaling

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.Configuration = "localhost:6379";
    });
```

**What it enables**:
- Load balancing across multiple servers
- Horizontal scaling
- Shared user state

### 2. Add Recording Service

```csharp
public interface IRecordingManager
{
    Task StartRecording(string roomId);
    Task StopRecording(string roomId);
    Task<byte[]> GetRecording(string recordingId);
}
```

**Integration**:
- MediaRecorder API on client
- Upload chunks via SignalR or HTTP
- Server-side stitching and storage

### 3. Add Chat Messaging

```csharp
// VideoCallHub.cs
public async Task SendChatMessage(string roomId, string message)
{
    var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
    var participants = _roomManager.GetRoomParticipants(roomId);
    
    foreach (var userId in participants)
    {
        await Clients.User(userId).SendAsync("ReceiveChatMessage", 
            new { From = user.UserId, Message = message });
    }
}
```

### 4. Migrate to SFU

```csharp
public interface IMediaServerManager
{
    Task<string> CreateRouter();
    Task<string> CreateTransport(string routerId, string userId);
    Task ConnectTransport(string transportId, string dtlsParameters);
    Task<string> Produce(string transportId, string kind, string rtpParameters);
    Task<string> Consume(string transportId, string producerId);
}
```

## Performance Considerations

### Bottlenecks

1. **Full Mesh Bandwidth**
   - Each user uploads N-1 streams
   - Solution: Migrate to SFU at scale

2. **SignalR Message Size**
   - Large SDP descriptions and ICE candidates
   - Solution: Already configured message size limits

3. **In-Memory State**
   - Limited by single server memory
   - Solution: Add Redis backplane

### Optimization Strategies

1. **Connection Pooling**
```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400; // 100KB
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
```

2. **Lazy Connection Establishment**
- Don't create peer connections until needed
- Close idle connections after timeout

3. **Video Quality Adaptation**
```typescript
const constraints = {
    video: {
        width: { ideal: 1280 },
        height: { ideal: 720 },
        frameRate: { ideal: 30, max: 30 }
    }
};
```

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public void AddUser_ShouldReturnTrue_WhenUserIsValid()
{
    var logger = Mock.Of<ILogger<UserManager>>();
    var userManager = new UserManager(logger);
    
    var result = userManager.AddUser("user1", "conn1");
    
    Assert.True(result);
}
```

### Integration Tests
```csharp
[Fact]
public async Task VideoCallHub_RegisterUser_ShouldBroadcastUserList()
{
    var hub = new VideoCallHub(userManager, roomManager, logger);
    
    await hub.RegisterUser("testUser");
    
    // Verify Clients.All.SendAsync was called
}
```

### E2E Tests (Playwright/Selenium)
```typescript
test('User can join video call', async ({ page }) => {
    await page.goto('https://localhost:5001');
    await page.fill('input[placeholder="User ID"]', 'Alice');
    await page.click('button:has-text("Register")');
    
    await expect(page.locator('video')).toBeVisible();
});
```

## Monitoring & Observability

### Logging Strategy

**Levels**:
- `Debug`: ICE candidate exchanges, detailed WebRTC events
- `Information`: User registration, room join/leave
- `Warning`: Failed operations, retries
- `Error`: Exceptions, critical failures

**Structured Logging**:
```csharp
_logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
```

### Metrics to Track

1. **Connection Metrics**
   - Active users
   - Active rooms
   - Peer connections per user

2. **Performance Metrics**
   - SignalR message latency
   - ICE gathering time
   - Connection establishment time

3. **Error Metrics**
   - Failed connections
   - Media device errors
   - SignalR disconnections

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<SignalRHealthCheck>("signalr")
    .AddCheck<UserManagerHealthCheck>("users");

app.MapHealthChecks("/health");
```

## Conclusion

This architecture provides:

? **Separation of Concerns**: Clear layer boundaries
? **SOLID Principles**: Applied throughout
? **Thread Safety**: Concurrent operations handled correctly
? **Extensibility**: Easy to add features
? **Testability**: Dependency injection and abstractions
? **Scalability**: Can evolve to Redis + SFU
? **Production-Ready**: Error handling, logging, monitoring

**Next Steps**:
1. Add authentication
2. Implement recording
3. Add chat messaging
4. Migrate to SFU for large rooms
5. Add Redis backplane for multi-server
