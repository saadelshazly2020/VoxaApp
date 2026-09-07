# SignalR Hub API Reference - Quick Fix Guide

## ?? Method Name Mapping

This document shows the correct method names to use when calling the SignalR Hub.

## ? Correct Method Names

### Hub Server Methods (C# - VideoCallHub.cs)

| Client Calls | Server Method | Description |
|--------------|---------------|-------------|
| `RegisterUser(userId)` | `RegisterUser` | Register a user |
| `CallUser(targetUserId, offer)` | `CallUser` | ? Send WebRTC offer (NOT SendOffer) |
| `AnswerCall(callerUserId, answer)` | `AnswerCall` | ? Send WebRTC answer (NOT SendAnswer) |
| `SendIceCandidate(targetUserId, candidate)` | `SendIceCandidate` | Send ICE candidate |
| `CreateRoom(roomId)` | `CreateRoom` | Create a new room |
| `JoinRoom(roomId)` | `JoinRoom` | Join existing room |
| `LeaveRoom(roomId)` | `LeaveRoom` | Leave current room |

### Server to Client Events

| Server Sends | Client Receives | Description |
|--------------|-----------------|-------------|
| `Registered(userId)` | `'Registered'` | User registered successfully |
| `UserListUpdated(users)` | `'UserListUpdated'` | Updated list of users |
| `RoomCreated(roomId)` | `'RoomCreated'` | Room created successfully |
| `RoomJoined(roomId, participants)` | `'RoomJoined'` | Joined room with participants |
| `RoomLeft(roomId)` | `'RoomLeft'` | Left room successfully |
| `ReceiveOffer(senderId, offer)` | `'ReceiveOffer'` | Received WebRTC offer |
| `ReceiveAnswer(senderId, answer)` | `'ReceiveAnswer'` | Received WebRTC answer |
| `ReceiveIceCandidate(senderId, candidate)` | `'ReceiveIceCandidate'` | Received ICE candidate |
| `UserJoinedRoom(userId, roomId)` | `'UserJoinedRoom'` | User joined your room |
| `UserLeftRoom(userId, roomId)` | `'UserLeftRoom'` | User left your room |
| `UserDisconnected(userId)` | `'UserDisconnected'` | User disconnected |
| `Error(message)` | `'Error'` | Error occurred |

## ?? Common Mistakes

### ? WRONG - Old Method Names
```typescript
// DON'T USE THESE:
await signalRService.connection.invoke('SendOffer', receiverId, offer);
await signalRService.connection.invoke('SendAnswer', receiverId, answer);
```

### ? CORRECT - Current Method Names
```typescript
// USE THESE:
await signalRService.connection.invoke('CallUser', receiverId, offer);
await signalRService.connection.invoke('AnswerCall', receiverId, answer);
```

## ?? How to Verify Method Names

### Check Hub Methods (Server)
```bash
# Open VideoCallHub.cs and look for public async Task methods
Get-Content "Hubs/VideoCallHub.cs" | Select-String "public async Task"
```

**Expected output**:
- `RegisterUser`
- `CallUser` ?
- `AnswerCall` ?
- `SendIceCandidate`
- `CreateRoom`
- `JoinRoom`
- `LeaveRoom`

### Check Client Service
```typescript
// In signalr.service.ts
async sendOffer(receiverId: string, offer: string): Promise<void> {
    await this.connection.invoke('CallUser', receiverId, offer); // ? Correct
}

async sendAnswer(receiverId: string, answer: string): Promise<void> {
    await this.connection.invoke('AnswerCall', receiverId, answer); // ? Correct
}
```

## ?? Debugging SignalR Method Errors

### Error: "Method does not exist"
```
HubException: Method does not exist.
```

**Cause**: Client is calling a method name that doesn't exist on the server.

**Solution**:
1. Check Hub method names in `Hubs/VideoCallHub.cs`
2. Verify client is calling the correct method name
3. Ensure method name is **case-sensitive** (PascalCase)

### Example Fix
```typescript
// If you see this error:
// Failed to invoke 'SendOffer' due to an error on the server

// Fix by changing to:
await this.connection.invoke('CallUser', receiverId, offer);
```

## ?? Complete WebRTC Call Flow

### 1-to-1 Call Sequence

```typescript
// User A (Caller)
1. await signalRService.sendOffer(userBId, offerSDP);
   // Invokes: CallUser(userBId, offerSDP)
   
// Server forwards to User B
2. Client B receives: 'ReceiveOffer' event
   
// User B (Callee) responds
3. await signalRService.sendAnswer(userAId, answerSDP);
   // Invokes: AnswerCall(userAId, answerSDP)
   
// Server forwards to User A
4. Client A receives: 'ReceiveAnswer' event

// Both users exchange ICE candidates
5. await signalRService.sendIceCandidate(otherUserId, candidate);
   // Invokes: SendIceCandidate(otherUserId, candidate)
```

## ?? Quick Reference Card

```
???????????????????????????????????????????????????
?  Client Method      ?  Server Method            ?
???????????????????????????????????????????????????
?  registerUser()     ?  RegisterUser             ?
?  sendOffer()        ?  CallUser        ? FIXED?
?  sendAnswer()       ?  AnswerCall      ? FIXED?
?  sendIceCandidate() ?  SendIceCandidate         ?
?  createRoom()       ?  CreateRoom               ?
?  joinRoom()         ?  JoinRoom                 ?
?  leaveRoom()        ?  LeaveRoom                ?
???????????????????????????????????????????????????
```

## ? Verification

After the fix, verify:

1. **Build succeeds**:
   ```bash
   dotnet build
   ```

2. **No console errors**:
   - Open browser console (F12)
   - Look for SignalR connection messages
   - Should NOT see "Method does not exist"

3. **Test call functionality**:
   - Register two users
   - Click "Call" button
   - Should establish WebRTC connection

## ?? If Issues Persist

1. **Restart backend**:
   ```bash
   dotnet run
   ```

2. **Clear browser cache**:
   - Press Ctrl+Shift+Delete
   - Clear cached files

3. **Check SignalR connection**:
   ```typescript
   // In browser console
   console.log('SignalR state:', signalRService.getConnectionState());
   // Should show: "Connected"
   ```

4. **Enable detailed logging**:
   ```typescript
   // In signalr.service.ts constructor
   .configureLogging(signalR.LogLevel.Debug) // Change to Debug
   ```

## ?? Related Files

- `Hubs/VideoCallHub.cs` - Server Hub methods
- `client-app/src/services/signalr.service.ts` - Client service
- `client-app/src/services/webrtc.service.ts` - WebRTC logic

## ?? Fix Applied

? **Updated** `client-app/src/services/signalr.service.ts`:
- `sendOffer()` now calls `CallUser` (was `SendOffer`)
- `sendAnswer()` now calls `AnswerCall` (was `SendAnswer`)

? **Build Status**: Successful
? **Breaking Changes**: None (internal service change only)

---

**Last Updated**: After SignalR method name fix
**Status**: ? Ready to test

**To verify the fix works**:
1. Start backend: `dotnet run`
2. Start frontend: `cd client-app && npm run dev`
3. Open two browser tabs
4. Register as different users
5. Try calling each other
6. Should work without "Method does not exist" error
