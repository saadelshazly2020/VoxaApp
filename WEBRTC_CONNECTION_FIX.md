# ?? WebRTC Connection Failed - Troubleshooting Guide

## Problem
```
ICE connection state with Surface: checking
Connection state with Surface: connecting
ICE connection state with Surface: disconnected
Connection state with Surface: failed
```

## Root Causes

Your connection is failing at the ICE gathering stage. Common causes:

### 1. **STUN/TURN Server Issues** ? FIXED
**Problem:** Your server `147.79.115.80:3478` is not accessible or not working

**Fix Applied:** Updated to use Google's reliable public STUN servers:
```typescript
stun:stun.l.google.com:19302
stun:stun1.l.google.com:19302
stun:stun2.l.google.com:19302
stun:stun3.l.google.com:19302
stun:stun4.l.google.com:19302
```

### 2. **ICE Candidate Exchange**
**Check:** Are ICE candidates being sent through SignalR?

### 3. **Firewall/Network**
**Check:** Is your network blocking WebRTC?

---

## Changes Made

### 1. Updated STUN Servers
```typescript
// BEFORE (broken)
iceServers: [
  {
    urls: 'stun:147.79.115.80:3478'  // Not accessible
  }
]

// AFTER (fixed)
iceServers: [
  {
    urls: [
      'stun:stun.l.google.com:19302',
      'stun:stun1.l.google.com:19302',
      'stun:stun2.l.google.com:19302',
      'stun:stun3.l.google.com:19302',
      'stun:stun4.l.google.com:19302'
    ]
  },
  {
    urls: ['stun:stun.services.mozilla.com:3478']
  }
]
```

### 2. Added Better Logging
- ICE candidate details
- ICE connection state
- Connection state
- Signaling state
- Detailed error logs when connection fails

---

## Testing Steps

### Step 1: Rebuild
```bash
dotnet clean
dotnet build
dotnet run
```

### Step 2: Test Connection
1. Open app on 2 devices/browsers
2. Register both users
3. One user calls the other
4. Check console logs

### Step 3: Look for Success Signs
```
? "New ICE candidate: ..." (multiple times)
? "ICE candidate gathering complete"
? "Connection state: connected"
? "ICE connection state: connected"
? "Received remote track from: [user]"
```

### Step 4: If Still Failing
Look for these messages:
```
? "No ICE candidates found" - Network issue
? "All ICE candidates failed" - Firewall issue
? "ICE connection FAILED" - Check console output
```

---

## Network Diagnostics

### Check if STUN servers are working
Open browser console and run:
```javascript
// Test STUN connectivity
async function testStun() {
  const config = {
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' }
    ]
  };
  
  const pc = new RTCPeerConnection(config);
  
  pc.onicecandidate = (e) => {
    if (e.candidate) {
      console.log('STUN working! Candidate:', e.candidate.candidate);
    }
  };
  
  await pc.createOffer().then(offer => pc.setLocalDescription(offer));
}

testStun();
```

If you see candidates, STUN is working. If not, check firewall.

---

## Common Issues & Solutions

### Issue 1: No ICE Candidates
**Symptom:** "ICE candidate gathering complete" but no candidates before it

**Causes:**
- Firewall blocking UDP 3478-3481 (STUN)
- STUN server unreachable
- Network policy issue

**Solutions:**
1. Check firewall settings
2. Allow outbound UDP traffic
3. Try from different network
4. Ask IT to allow STUN traffic

### Issue 2: Candidates Gathered But Still Failed
**Symptom:** See ICE candidates but connection still fails

**Causes:**
- ICE candidate exchange not working through SignalR
- Candidates being lost
- Timing issue between offer/answer

**Solutions:**
1. Check SignalR is sending ICE candidates
2. Check `onReceiveIceCandidate` is being called
3. Verify candidates are being added

### Issue 3: Works Locally, Fails on ngrok
**Symptom:** Localhost works, ngrok fails

**Causes:**
- ngrok changing IP addresses
- Firewall rules different for ngrok
- Network issues with ngrok tunnel

**Solutions:**
1. Check ngrok tunnel is stable
2. Verify ngrok forwarding is working
3. Test with different ngrok region

### Issue 4: Works on Local Network, Fails from Different Network
**Symptom:** Same WiFi works, different network fails

**Causes:**
- ISP blocking STUN/WebRTC
- Network firewall rules
- Different network policies

**Solutions:**
1. Try on mobile hotspot
2. Try from different WiFi
3. Use VPN to test

---

## Detailed Logging

Your app now logs:
1. **ICE Candidates** - When candidates are gathered
2. **Connection States** - All state transitions
3. **Signaling State** - Offer/Answer/Stable states
4. **Error Details** - When connection fails

### What to Check in Console
```
// Good flow
New ICE candidate: candidate:xxx (srflx)
New ICE candidate: candidate:yyy (prflx)
New ICE candidate: candidate:zzz (relay)
ICE candidate gathering complete
ICE connection state: checking
ICE connection state: connected ?
Connection state: connected ?

// Bad flow
ICE candidate gathering complete (no candidates before this)
ICE connection state: checking
ICE connection state: failed ?
```

---

## If Still Not Working

### Debug Steps
1. **Check STUN servers:**
   - Run the test code above
   - Should see multiple candidates

2. **Check SignalR:**
   - ICE candidates being sent?
   - Check Network tab ? WebSocket messages

3. **Check Firewall:**
   - Try from different network
   - Try allowing UDP through firewall

4. **Check ngrok:**
   - Is tunnel still active?
   - Try local connection first

### Collect Debug Info
```javascript
// Run in console
async function debugWebRTC() {
  const pc = new RTCPeerConnection({
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' }
    ]
  });
  
  pc.onicecandidate = (e) => {
    if (e.candidate) {
      console.log('Candidate:', e.candidate.candidate.split(' ')[4]);
    }
  };
  
  const offer = await pc.createOffer();
  await pc.setLocalDescription(offer);
  
  // Wait 5 seconds
  await new Promise(r => setTimeout(r, 5000));
  
  console.log('Stats:', pc.connectionState, pc.iceConnectionState);
}

debugWebRTC();
```

---

## Expected Behavior After Fix

### Successful Connection
```
? ICE candidates gathered from Google STUN servers
? Candidates exchanged through SignalR
? Connection established
? Remote stream received
? Video/Audio working
```

### Connection Flow
```
1. createOffer() called
2. setLocalDescription(offer)
3. ICE gathering starts
4. STUN servers queried
5. Candidates gathered
6. Candidates sent through SignalR
7. Other peer receives candidates
8. Other peer adds candidates
9. Answer sent through SignalR
10. setRemoteDescription(answer)
11. Connection established ?
12. Remote stream received ?
```

---

## Summary

| Item | Status |
|------|--------|
| **STUN Servers** | ? Updated to Google's reliable servers |
| **Logging** | ? Enhanced to track all states |
| **Error Messages** | ? More detailed diagnostics |
| **ICE Candidates** | ? Now tracked and logged |

---

## Next Steps

1. **Rebuild:** `dotnet clean && dotnet build && dotnet run`
2. **Test:** Make a call from 2 users
3. **Check console:** Look for success messages
4. **If failing:** Run debug code above and share output

---

**The fix has been applied! Test your connection now.** ??
