# Quick Start Guide

Get your video chat application running in **5 minutes**!

## Prerequisites Check

Before starting, ensure you have:
- ? **.NET 9 SDK** installed ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- ? Modern web browser (Chrome, Firefox, Edge, Safari)
- ? Working camera and microphone

### Verify .NET Installation

```bash
dotnet --version
# Should output: 9.x.x
```

## Step 1: Run the Application

```bash
# Navigate to project directory
cd VideoChatingApp.WebRTC

# Restore dependencies (first time only)
dotnet restore

# Run the application
dotnet run
```

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

## Step 2: Open in Browser

1. Open your browser to: `https://localhost:5001`
2. Accept the security warning (self-signed certificate in development)
3. You should see the Video Chat Application interface

## Step 3: Test with One User

### Allow Permissions

1. Enter a **User ID** (e.g., "Alice")
2. Click **Register**
3. Browser will prompt for camera/microphone access
4. Click **Allow**

You should now see:
- ? Status shows "Connected"
- ? Your local video feed appears
- ? Your user ID displayed

## Step 4: Test 1-to-1 Call

### Open Second Browser Window

1. Open a **new browser window** (or incognito/private window)
2. Go to `https://localhost:5001`
3. Register with a different name (e.g., "Bob")
4. Allow camera/microphone access

### Make the Call

**From Alice's window**:
1. You should see "Bob" in the **Connected Users** list
2. Click the **Call** button next to Bob's name
3. Wait a few seconds for connection

**Result**:
- ? Alice sees Bob's video
- ? Bob sees Alice's video
- ? Two-way audio/video working

## Step 5: Test Multi-User Room

### Create a Room

**From Alice's window**:
1. In the **Room ID** input, type: `test-room`
2. Click **Create** button
3. You should see "Room test-room created"

### Join the Room

**From Bob's window**:
1. In the **Room ID** input, type: `test-room`
2. Click **Join** button
3. Wait for connection

**From a third window** (register as "Charlie"):
1. Type `test-room` in Room ID
2. Click **Join**
3. You should now see all three participants

## Step 6: Test Controls

### Audio/Video Controls

Try these buttons in any window:
- **?? Mute/Unmute**: Toggle your microphone
- **?? Stop/Start Video**: Toggle your camera
- **?? Leave Room**: Exit the current room
- **?? End Call**: End 1-to-1 call

### Verify

- When you mute, others don't hear you
- When you stop video, others see black screen
- Leaving room closes all connections

## Common First-Time Issues

### Issue: "Permission Denied"

**Solution**: 
- Click the ?? padlock in address bar
- Allow camera and microphone
- Refresh the page

### Issue: "HTTPS Certificate Warning"

**Solution**: Click **Advanced** ? **Proceed to localhost**

This is normal for development (self-signed certificate).

### Issue: "Can't See Other User"

**Checklist**:
- ? Both users registered?
- ? Both users in same room?
- ? Check browser console (F12) for errors
- ? Wait 5-10 seconds for connection

### Issue: "No Video/Audio"

**Quick Fixes**:
1. Check browser permissions
2. Verify camera/mic work in other apps
3. Try different browser
4. Check console (F12) for errors

## What's Next?

### Customize the TURN Server

1. Open `wwwroot/src/services/webrtc.service.ts`
2. Update line 17-21 with your TURN server:

```typescript
{ 
    urls: 'turn:YOUR_SERVER_IP:3478', 
    username: 'your_username', 
    credential: 'your_password' 
}
```

### Deploy to Production

See [DEPLOYMENT.md](DEPLOYMENT.md) for:
- Azure App Service deployment
- Docker containerization
- IIS setup
- TURN server configuration

### Add Features

The architecture supports easy extensions:
- Screen sharing
- Chat messaging
- Recording
- User authentication

See [ARCHITECTURE.md](ARCHITECTURE.md) for extensibility details.

## Development Mode

For automatic reload on code changes:

```bash
dotnet watch run
```

Now changes to `.cs` files automatically rebuild the application.

## Testing Checklist

Use this checklist to verify everything works:

- [ ] Single user can register
- [ ] Local video/audio displays
- [ ] Two users can call each other
- [ ] Audio works both directions
- [ ] Video works both directions
- [ ] Three users can join a room
- [ ] All users see each other in room
- [ ] Mute/unmute works
- [ ] Video on/off works
- [ ] Leave room closes connections
- [ ] User disconnection detected
- [ ] Reconnection works after network drop

## Architecture Overview

```
Browser                     .NET Server              WebRTC
???????????                ????????????            ??????????
? Vue.js  ???SignalR????????  Hub     ?            ? P2P    ?
? App     ?   (WebSocket)  ?          ?            ? Audio/ ?
?         ?                ? Managers ?            ? Video  ?
? WebRTC  ?????????????????????????????????????????? Stream ?
? Service ?         (Direct Connection)            ?        ?
???????????                ????????????            ??????????
```

**Key Technologies**:
- **Frontend**: Vue.js 3 + TypeScript
- **Backend**: ASP.NET Core 9 + SignalR
- **Real-time**: WebRTC for media, SignalR for signaling

## Useful Commands

```bash
# Build without running
dotnet build

# Clean build artifacts
dotnet clean

# Run with specific port
dotnet run --urls "https://localhost:7001"

# Run in production mode
dotnet run --environment Production

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## Browser Console Commands

Open console (F12) and try:

```javascript
// Check WebRTC support
console.log('WebRTC supported:', !!window.RTCPeerConnection);

// List available devices
navigator.mediaDevices.enumerateDevices()
    .then(devices => console.log(devices));

// Check SignalR connection state
// (After registering)
console.log('SignalR state:', connection.state);
```

## Performance Tips

### For Better Quality
1. Use wired internet connection
2. Close unnecessary browser tabs
3. Ensure good lighting for video
4. Use headphones to prevent echo

### For Lower Bandwidth
```typescript
// In webrtc.service.ts, adjust:
video: {
    width: { ideal: 640 },
    height: { ideal: 480 },
    frameRate: { max: 24 }
}
```

## Project Structure Quick Reference

```
VideoChatingApp.WebRTC/
??? Core/                    # Domain models & interfaces
?   ??? Models/
?   ??? Interfaces/
??? Managers/                # Business logic
??? Hubs/                    # SignalR communication
??? wwwroot/                 # Frontend
?   ??? src/
?   ?   ??? components/      # Vue components
?   ?   ??? services/        # Business logic
?   ?   ??? models/          # TypeScript types
?   ??? index.html
??? Program.cs               # App startup
??? README.md                # Full documentation
```

## Getting Help

1. **Documentation**:
   - [README.md](README.md) - Complete guide
   - [ARCHITECTURE.md](ARCHITECTURE.md) - System design
   - [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common issues

2. **Debugging**:
   - Browser Console (F12)
   - Server logs in terminal
   - `chrome://webrtc-internals` for WebRTC stats

3. **Common Questions**:

**Q: Can I use different ports?**
```bash
dotnet run --urls "https://localhost:7001"
```

**Q: How do I test from different devices?**
Replace `localhost` with your machine's IP address:
```
https://192.168.1.100:5001
```

**Q: How many users can join?**
Current full-mesh architecture: 4-6 users recommended.
For more users, see [ARCHITECTURE.md](ARCHITECTURE.md) for SFU migration.

## Success Indicators

You know it's working when:
- ? Status bar shows "Connected" in green
- ? Local video feed displays immediately
- ? Other users appear in user list
- ? Remote video connects within 5 seconds
- ? Two-way audio/video confirmed
- ? No errors in browser console

## Next Steps

1. ? Complete this quick start
2. ?? Read [README.md](README.md) for full features
3. ??? Review [ARCHITECTURE.md](ARCHITECTURE.md) to understand design
4. ?? Check [DEPLOYMENT.md](DEPLOYMENT.md) for production
5. ??? Extend with your own features

---

**Congratulations! ??**

You now have a working production-ready video chat application!

**Time to complete**: ~5 minutes
**Lines of code**: 2,000+
**Technologies mastered**: ASP.NET Core 9, SignalR, Vue.js 3, TypeScript, WebRTC

Happy coding! ????
