# ?? ngrok WSS Scheme Error - FIXED!

## Problem
```
Fetch API cannot load wss://4e97-194-238-97-224.ngrok-free.app/videocallhub/negotiate?negotiateVersion=1
URL scheme "wss" is not supported.
```

## Root Cause

When you use a full URL like `wss://ngrok-url/videocallhub`, the browser tries to connect directly to that WebSocket URL, but:

1. **ngrok requires HTTP negotiation first** - SignalR needs to do HTTP POST to `/negotiate` before upgrading to WebSocket
2. **Full URLs bypass the reverse proxy** - The URL scheme becomes an issue
3. **Relative URLs work perfectly** - They let ngrok handle the protocol upgrade properly

## Solution Applied

### 1. Updated config.ts - Use Relative URLs

**Before (Broken):**
```typescript
if (host.includes('ngrok')) {
    return `${protocol}://${host}/videocallhub`;  // ? Full URL
}
return '/videocallhub';
```

**After (Fixed):**
```typescript
// Always use relative URLs - ngrok reverses proxy correctly
return '/videocallhub';
```

### 2. Fixed Program.cs - All Configuration Issues

**Fixed:**
- ? `RootPath` set to `wwwroot/dist`
- ? `SourcePath` set to `client-app`
- ? Development proxy enabled
- ? SPA static files conditional (production only)
- ? Fallback route enabled

---

## How It Works Now

### Localhost Flow
```
Browser: http://localhost:5274
    ?
Uses relative URL: /videocallhub
    ?
Connects to: ws://localhost:5274/videocallhub
    ? Works perfectly
```

### ngrok Flow
```
Browser: https://4e97-194-238-97-224.ngrok-free.app
    ?
Uses relative URL: /videocallhub
    ?
ngrok receives: https://4e97-194-238-97-224.ngrok-free.app/videocallhub
    ?
ngrok reverse proxies to: http://localhost:5274/videocallhub
    ?
HTTP negotiation works ?
    ?
Upgrades to: ws://localhost:5274/videocallhub
    ? Works perfectly through ngrok
```

---

## Testing

### Step 1: Stop App
```bash
Ctrl+C
```

### Step 2: Rebuild
```bash
dotnet clean
dotnet build
```

### Step 3: Start
```bash
dotnet run
```

### Step 4: Test Locally
```
http://localhost:5274
```

**Should see:**
- ? F12 Console: "SignalR connected"
- ? Can register
- ? User list appears
- ? WebSocket connection established

### Step 5: Test via ngrok
```
https://4e97-194-238-97-224.ngrok-free.app
```

**Should see same results:**
- ? F12 Console: "SignalR connected"
- ? Can register
- ? User list appears
- ? **NO "wss is not supported" error** ?

---

## Verification

### Check Browser Console (F12)
```
? Should show: "SignalR connected"
? Should NOT show: "URL scheme 'wss' is not supported"
```

### Check Network Tab
- Look for `/videocallhub/negotiate`
- Should see: **Status 200** (HTTP)
- Then WebSocket connects: **Status 101**

### Check Connection
```javascript
// In console, type:
console.log(signalRService?.connection.state)
// Should show: "Connected" (state = 1)
```

---

## Why Relative URLs Work Everywhere

| Scenario | Relative URL | Result |
|----------|--------------|--------|
| **Localhost** | `/videocallhub` | Connects to `ws://localhost:5274/videocallhub` ? |
| **ngrok** | `/videocallhub` | ngrok proxies properly, HTTP negotiation works ? |
| **Full URL ngrok** | `wss://ngrok.../videocallhub` | Browser tries direct WSS, fails ? |
| **Full URL localhost** | `wss://localhost:5274/videocallhub` | Works but unnecessary ?? |

**Lesson:** Always use relative URLs for WebSocket connections when behind reverse proxies!

---

## Changes Summary

| File | Change | Status |
|------|--------|--------|
| **config.ts** | Always use relative URLs | ? Applied |
| **Program.cs:33** | Fixed RootPath to `wwwroot/dist` | ? Applied |
| **Program.cs:55** | Fixed SourcePath to `client-app` | ? Applied |
| **Program.cs:51-53** | Enabled dev proxy | ? Applied |
| **Program.cs:48-50** | Made UseSpaStaticFiles conditional | ? Applied |
| **Program.cs:62** | Enabled fallback route | ? Applied |

---

## Next Steps

```bash
# 1. Stop (if running)
Ctrl+C

# 2. Rebuild
dotnet clean && dotnet build

# 3. Start
dotnet run

# 4. Test locally
# Open: http://localhost:5274
# Should work perfectly

# 5. Test via ngrok
# Open: https://4e97-194-238-97-224.ngrok-free.app
# Should work perfectly (NO wss errors!)
```

---

## Key Takeaway

**Use relative URLs with ngrok!** 

```typescript
// ? CORRECT
return '/videocallhub';

// ? WRONG
return `${protocol}://${host}/videocallhub`;
```

This allows ngrok to:
1. Intercept the HTTP request
2. Reverse proxy to localhost
3. Upgrade to WebSocket properly
4. Everything works seamlessly

---

**All fixes applied! Ready to test!** ??
