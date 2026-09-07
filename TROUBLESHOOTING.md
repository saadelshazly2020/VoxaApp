# Troubleshooting Guide

## Common Issues and Solutions

### 1. Browser Permissions

#### Issue: Camera/Microphone Permission Denied
**Symptoms**:
- Error message: "Permission denied. Please allow camera and microphone access"
- No local video feed

**Solutions**:

**Chrome/Edge**:
1. Click the camera icon in the address bar
2. Select "Always allow https://yourdomain.com to access your camera and microphone"
3. Click "Done"
4. Refresh the page

**Firefox**:
1. Click the camera icon in the address bar
2. Check "Remember this decision"
3. Click "Allow"
4. Refresh the page

**Safari**:
1. Safari > Settings for This Website
2. Camera: Allow
3. Microphone: Allow
4. Refresh the page

**Reset Permissions**:
- Chrome: Settings > Privacy and security > Site settings > Camera/Microphone
- Firefox: Settings > Privacy & Security > Permissions
- Safari: Safari > Settings > Websites > Camera/Microphone

#### Issue: No Camera/Microphone Found
**Symptoms**:
- Error: "No camera or microphone found"
- Device list is empty

**Solutions**:
1. Check if device is connected
2. Verify device works in other applications
3. Restart browser
4. Check system privacy settings:
   - Windows: Settings > Privacy > Camera/Microphone
   - macOS: System Settings > Privacy & Security > Camera/Microphone
5. Update device drivers

### 2. WebRTC Connection Issues

#### Issue: Peer Connection Fails
**Symptoms**:
- Call doesn't connect
- "Connection failed" message
- ICE connection state stuck at "checking"

**Solutions**:

**Check TURN Server**:
```bash
# Test TURN server connectivity
telnet 172.24.32.1 3478

# Or use online tool:
# https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/
```

**Verify Configuration**:
```typescript
// In webrtc.service.ts
configuration: {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { 
            urls: 'turn:YOUR_TURN_SERVER:3478',  // Verify this is correct
            username: 'webrtcuser', 
            credential: 'StrongPassword123' 
        }
    ]
}
```

**Check Network**:
- Corporate firewall may block WebRTC
- Try from different network
- Use mobile hotspot to test

**Enable Debug Logging**:
```typescript
// Add to webrtc.service.ts constructor
console.log('WebRTC Configuration:', this.configuration);

// In SignalR service
.configureLogging(LogLevel.Debug)  // Change from Information
```

#### Issue: Video Works But Audio Doesn't
**Symptoms**:
- Video stream visible
- No audio from remote peer
- Audio icon shows unmuted

**Solutions**:

1. **Check Browser Console**:
```javascript
// Look for errors like:
// "Track has ended, cannot add track"
// "Invalid audio constraints"
```

2. **Verify Audio Track**:
```typescript
// Add to webrtc.service.ts
console.log('Audio tracks:', this.localStream?.getAudioTracks());
console.log('Video tracks:', this.localStream?.getVideoTracks());
```

3. **Test Audio Device**:
- Open System Settings > Sound
- Speak into microphone, verify level indicator moves
- Try different microphone in browser settings

4. **Check Constraints**:
```typescript
// Explicitly request audio
await navigator.mediaDevices.getUserMedia({
    video: true,
    audio: {
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true
    }
});
```

#### Issue: Video Autoplay Blocked
**Symptoms**:
- Remote video shows but doesn't play
- Console error: "play() failed because the user didn't interact with the document first"

**Solution**: Already handled in `VideoGrid.ts`:
```typescript
video.play().catch(error => {
    console.warn('Autoplay failed:', error);
    // User must click video to start
    video.onclick = () => {
        video.play();
        video.onclick = null;
    };
});
```

**User Action**: Click on the video element to start playback.

### 3. SignalR Connection Issues

#### Issue: SignalR Connection Fails
**Symptoms**:
- Status shows "Disconnected"
- Error in console: "Failed to start connection"

**Solutions**:

1. **Check Server is Running**:
```bash
# Verify server is running
netstat -an | grep 5001  # Linux/macOS
netstat -an | findstr 5001  # Windows
```

2. **Verify Hub URL**:
```typescript
// In signalr.service.ts
constructor(hubUrl: string = '/videocallhub') {
    // Should match hub endpoint in Program.cs:
    // app.MapHub<VideoCallHub>("/videocallhub");
}
```

3. **Check CORS Configuration**:
```csharp
// Program.cs - verify CORS is enabled
app.UseCors("AllowAll");
```

4. **Enable WebSockets**:
- IIS: Enable WebSocket Protocol in Windows Features
- Nginx: Verify `proxy_set_header Upgrade $http_upgrade;`

5. **Check Browser Console**:
```javascript
// Look for specific errors:
// "WebSocket connection failed"
// "404 Not Found" - wrong URL
// "403 Forbidden" - CORS issue
```

#### Issue: SignalR Disconnects Frequently
**Symptoms**:
- Connection drops every few minutes
- Status flickers between Connected/Reconnecting

**Solutions**:

1. **Increase Timeout**:
```csharp
// Program.cs
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);  // Increase from default
});
```

2. **Check Network Stability**:
- Test with `ping` command
- Look for packet loss
- Try different network

3. **Verify Load Balancer Configuration**:
- Enable sticky sessions
- Increase timeout values
- Check WebSocket support

#### Issue: Messages Not Received
**Symptoms**:
- User list doesn't update
- Call invitations not received
- ICE candidates not exchanged

**Solutions**:

1. **Verify Event Handlers**:
```typescript
// In VideoChat.ts - ensure all handlers are registered
signalRService.on('UserListUpdated', (users) => {
    console.log('User list updated:', users);  // Add logging
});
```

2. **Check Server Logs**:
```bash
# Look for errors in console output
dotnet run --verbosity detailed
```

3. **Verify User Registration**:
```typescript
// Before calling other methods, ensure:
if (!isRegistered.value) {
    await registerUser();
}
```

### 4. Room/Call Issues

#### Issue: Can't Join Room
**Symptoms**:
- "Room not found" error
- Room join button does nothing

**Solutions**:

1. **Verify Room Exists**:
- Another user must create the room first
- Room ID is case-sensitive

2. **Check for JavaScript Errors**:
```javascript
// Open browser console (F12)
// Look for errors in joinRoom() method
```

3. **Verify SignalR Connection**:
```typescript
if (!signalRService.isConnected()) {
    console.error('SignalR not connected');
}
```

#### Issue: Other Participants Not Visible
**Symptoms**:
- Joined room but no video feeds
- User list shows other users

**Solutions**:

1. **Check WebRTC Negotiation**:
```typescript
// Add to webrtc.service.ts
console.log('Peer connections:', this.peerConnections);
console.log('Remote streams:', Object.keys(this.remoteStreams));
```

2. **Verify ICE Candidates Exchange**:
```typescript
// Check console for:
// "Sending ICE candidate to {userId}"
// "Received ICE candidate from {userId}"
```

3. **Check Connection State**:
```typescript
// Add event listener
peerConnection.onconnectionstatechange = () => {
    console.log('Connection state:', peerConnection.connectionState);
    // Should eventually reach "connected"
};
```

#### Issue: One-Way Video
**Symptoms**:
- You see other user's video
- They don't see yours (or vice versa)

**Solutions**:

1. **Verify Track Addition**:
```typescript
// In createPeerConnection()
if (this.localStream) {
    this.localStream.getTracks().forEach(track => {
        console.log('Adding track:', track.kind, track.id);
        peerConnection.addTrack(track, this.localStream!);
    });
}
```

2. **Check Track Enabled State**:
```typescript
// Verify tracks aren't disabled
this.localStream?.getTracks().forEach(track => {
    console.log(track.kind, 'enabled:', track.enabled);
});
```

3. **Review SDP Offers/Answers**:
```typescript
// Log SDP to check media sections
console.log('Offer SDP:', offer.sdp);
console.log('Answer SDP:', answer.sdp);
// Should contain m=audio and m=video lines
```

### 5. Performance Issues

#### Issue: High CPU Usage
**Symptoms**:
- Browser becomes slow
- Fan runs constantly
- CPU usage > 80%

**Solutions**:

1. **Reduce Video Quality**:
```typescript
// In initLocalMedia()
const constraints = {
    video: {
        width: { ideal: 640 },    // Lower from 1280
        height: { ideal: 480 },   // Lower from 720
        frameRate: { max: 24 }    // Lower from 30
    },
    audio: true
};
```

2. **Limit Participants**:
- Full mesh doesn't scale beyond 4-6 users
- Consider migrating to SFU for larger groups

3. **Close Unused Connections**:
```typescript
// Ensure connections are closed when leaving
webRTCService.closeAllConnections();
```

4. **Use Hardware Acceleration**:
- Chrome: Settings > System > Use hardware acceleration when available
- Restart browser after enabling

#### Issue: Poor Video Quality
**Symptoms**:
- Blurry or pixelated video
- Choppy/stuttering video

**Solutions**:

1. **Check Network Bandwidth**:
```bash
# Test speed
speedtest-cli  # Linux/macOS
# Or use https://fast.com
```

2. **Adjust Video Constraints**:
```typescript
video: {
    width: { ideal: 1280 },
    height: { ideal: 720 },
    frameRate: { ideal: 30 },
    facingMode: 'user'
}
```

3. **Monitor Connection Stats**:
```typescript
// Add stats monitoring
setInterval(async () => {
    const stats = await peerConnection.getStats();
    stats.forEach(report => {
        if (report.type === 'inbound-rtp' && report.kind === 'video') {
            console.log('Video bitrate:', report.bytesReceived);
            console.log('Packets lost:', report.packetsLost);
        }
    });
}, 5000);
```

4. **Check CPU Throttling**:
- Browser may reduce quality when CPU is high
- Close other tabs/applications

### 6. Development Issues

#### Issue: Hot Reload Not Working
**Symptoms**:
- Changes to .cs files don't reflect
- Need to restart server manually

**Solution**:
```bash
# Use watch mode
dotnet watch run
```

#### Issue: TypeScript Errors Not Showing
**Symptoms**:
- JavaScript errors at runtime
- No compile-time warnings

**Solution**:
Currently using plain TypeScript files without compilation. To add build step:

1. **Install TypeScript**:
```bash
npm init -y
npm install --save-dev typescript
```

2. **Create tsconfig.json**:
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "module": "ES2020",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "moduleResolution": "node"
  },
  "include": ["wwwroot/src/**/*"]
}
```

3. **Add Build Script**:
```json
"scripts": {
  "build": "tsc"
}
```

#### Issue: SignalR Package Not Found
**Symptoms**:
- Build error: Cannot find package 'Microsoft.AspNetCore.SignalR'

**Solution**:
```bash
dotnet add package Microsoft.AspNetCore.SignalR
dotnet restore
```

Note: SignalR is included in ASP.NET Core 9 by default, but explicit package may be needed in some cases.

### 7. Production Issues

#### Issue: HTTPS Certificate Error
**Symptoms**:
- Browser shows "Not Secure"
- WebRTC doesn't work (requires HTTPS)

**Solution**:

**Development**:
```bash
dotnet dev-certs https --trust
```

**Production**:
```bash
# Use Let's Encrypt
certbot certonly --standalone -d yourdomain.com

# Configure in appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/etc/letsencrypt/live/yourdomain.com/fullchain.pem",
          "KeyPath": "/etc/letsencrypt/live/yourdomain.com/privkey.pem"
        }
      }
    }
  }
}
```

#### Issue: 500 Internal Server Error
**Symptoms**:
- Server returns 500 error
- No specific error message

**Solution**:

1. **Enable Detailed Errors**:
```csharp
// Program.cs (Development only!)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

2. **Check Logs**:
```bash
# Application logs
tail -f /var/log/videochat/app.log

# IIS logs
type C:\inetpub\logs\LogFiles\W3SVC1\u_exXXXXXX.log
```

3. **Verify Dependencies**:
```bash
dotnet restore
dotnet build
```

#### Issue: TURN Server Not Working
**Symptoms**:
- ICE connection state: "failed"
- Only works on local network

**Solution**:

1. **Test TURN Server**:
```bash
# Install turnutils
sudo apt-get install coturn

# Test TURN
turnutils_uclient -v -t -u webrtcuser -w StrongPassword123 172.24.32.1
```

2. **Verify Firewall Rules**:
```bash
# Allow TURN ports
sudo ufw allow 3478/tcp
sudo ufw allow 3478/udp
sudo ufw allow 49152:65535/udp  # TURN relay ports
```

3. **Check TURN Logs**:
```bash
sudo journalctl -u coturn -f
```

## Debugging Tools

### Browser Developer Tools

**Chrome DevTools**:
```
chrome://webrtc-internals
```
Shows detailed WebRTC statistics, connection graphs, and diagnostics.

**Firefox about:webrtc**:
```
about:webrtc
```
Similar to Chrome, shows peer connection details.

### Network Monitoring

**Wireshark**:
Filter for WebRTC traffic:
```
udp.port == 3478 || stun || turn
```

**tcpdump**:
```bash
sudo tcpdump -i any port 3478 -w webrtc.pcap
```

### SignalR Tracing

**Enable Detailed Logging**:
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
```

## Getting Help

### Information to Provide

When reporting issues, include:

1. **Browser and Version**:
   - Chrome 120.0.6099.109
   - Firefox 121.0

2. **Operating System**:
   - Windows 11
   - macOS 14.2
   - Ubuntu 22.04

3. **Console Errors**:
   - Copy full error stack trace
   - Include warnings

4. **Network Information**:
   - Behind corporate firewall? (yes/no)
   - Using VPN? (yes/no)

5. **Steps to Reproduce**:
   1. Open browser
   2. Register as "Alice"
   3. Click "Call Bob"
   4. Connection fails

6. **Server Logs**:
   - Include relevant server console output

### Useful Commands

**Check .NET Version**:
```bash
dotnet --version
```

**Check Running Processes**:
```bash
# Linux/macOS
ps aux | grep dotnet

# Windows
tasklist | findstr dotnet
```

**Check Port Usage**:
```bash
# Linux/macOS
lsof -i :5001

# Windows
netstat -ano | findstr :5001
```

**Clear Browser Cache**:
- Chrome: Ctrl+Shift+Delete
- Firefox: Ctrl+Shift+Delete
- Edge: Ctrl+Shift+Delete

### Known Limitations

1. **Full Mesh Scaling**: Limited to 4-6 users before performance degrades
2. **Mobile Support**: May require different constraints for iOS Safari
3. **Screen Sharing**: Not implemented in current version
4. **Recording**: Not implemented in current version
5. **Simulcast**: Not implemented (would improve quality)

## Emergency Fixes

### Quick Reset Procedure

1. **Stop Application**:
```bash
# Press Ctrl+C in terminal
```

2. **Clear Browser**:
- Close all tabs
- Clear cache and cookies
- Restart browser

3. **Rebuild Application**:
```bash
dotnet clean
dotnet restore
dotnet build
dotnet run
```

4. **Reset Development Certificates**:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

5. **Check No Other Process Using Port**:
```bash
# Kill process on port 5001
# Linux/macOS
lsof -ti:5001 | xargs kill -9

# Windows
FOR /F "tokens=5" %P IN ('netstat -ano ^| findstr :5001') DO TaskKill /PID %P /F
```

---

**Last Updated**: 2024
**Version**: 1.0

For additional support, review:
- README.md for general usage
- ARCHITECTURE.md for system design
- DEPLOYMENT.md for production setup
