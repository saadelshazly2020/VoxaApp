# ?? SignalR Negotiation Error Fix

## Problem
```
Error: Failed to complete negotiation with the server: TypeError: Failed to fetch
```

This error means SignalR can't complete the connection handshake with the server.

## Root Causes Found & Fixed

### Issue 1: Wrong Transport Configuration
**Problem:** Using `WebSockets` only with `skipNegotiation: false` doesn't work. SignalR needs HTTP negotiation first.

**Fixed:** Updated to use both WebSockets and LongPolling transports.

### Issue 2: Wrong SPA Static Files Path  
**Problem:** `configuration.RootPath = "client-app/dist"` - Files are in `wwwroot/dist`

**Fixed:** Changed to `configuration.RootPath = "wwwroot/dist"`

### Issue 3: SPA Proxy Commented Out
**Problem:** Development mode proxy wasn't enabled, so it couldn't find Vue dev server

**Fixed:** Uncommented the development proxy configuration

### Issue 4: SourcePath Wrong
**Problem:** `spa.Options.SourcePath = "client-app/dist"` - Should be `"client-app"`

**Fixed:** Changed to `spa.Options.SourcePath = "client-app"`

### Issue 5: Fallback Route Commented Out
**Problem:** SPA routing fallback wasn't configured

**Fixed:** Uncommented `app.MapFallbackToFile("index.html")`

## Changes Made

### 1. signalr.service.ts - Fixed Transport Configuration

```typescript
// BEFORE (broken)
.withUrl(url, {
    skipNegotiation: false,
    transport: signalR.HttpTransportType.WebSockets,
    withCredentials: true
})

// AFTER (fixed)
.withUrl(url, {
    withCredentials: true,
    transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
})
```

### 2. Program.cs - Fixed All Configuration Issues

**SpaStaticFiles path:**
```csharp
// BEFORE
configuration.RootPath = "client-app/dist";

// AFTER  
configuration.RootPath = "wwwroot/dist";
```

**SPA proxy enabled:**
```csharp
// BEFORE (commented out)
//if (app.Environment.IsDevelopment())
//{
//    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
//}

// AFTER (enabled)
if (app.Environment.IsDevelopment())
{
    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
}
```

**SourcePath corrected:**
```csharp
// BEFORE
spa.Options.SourcePath = "client-app/dist";

// AFTER
spa.Options.SourcePath = "client-app";
```

**UseSpaStaticFiles conditional:**
```csharp
// BEFORE (always called)
app.UseSpaStaticFiles();

// AFTER (only in production)
if (!app.Environment.IsDevelopment())
{
    app.UseSpaStaticFiles();
}
```

**Fallback enabled:**
```csharp
// BEFORE (commented out)
//app.MapFallbackToFile("index.html");

// AFTER (enabled)
app.MapFallbackToFile("index.html");
```

## Recovery Steps

### Step 1: Stop Running App
```bash
# Press Ctrl+C in your terminal
```

### Step 2: Clean and Rebuild
```bash
dotnet clean
dotnet build
```

### Step 3: Start Fresh
```bash
dotnet run
```

### Step 4: Test Connection

**Open in browser:**
```
http://localhost:5274
```

**Check browser console (F12):**
- Should see "SignalR connected" message
- No red "Failed to complete negotiation" errors
- Should be able to register and see users list

**Test with ngrok (if using):**
```
https://4e97-194-238-97-224.ngrok-free.app
```

Should connect via WebSocket to your ngrok URL.

## Verification Checklist

After restart, verify:

- [ ] Browser opens without errors
- [ ] F12 Console shows "SignalR connected"
- [ ] Can enter username and click "Register"
- [ ] User list appears with your username
- [ ] No WebSocket errors in console
- [ ] Network tab shows WebSocket connection established
- [ ] Can see other connected users

## Connection Flow (After Fix)

```
Browser
    ?
SignalR tries WebSocket first
    ?
If WebSocket fails, falls back to LongPolling
    ?
HTTP negotiation succeeds (both transports work)
    ?
Connection established ?
```

## Why These Fixes Work

| Fix | Why |
|-----|-----|
| **Mixed transports** | Allows fallback if one fails |
| **Correct paths** | Files are in right location |
| **Enabled proxy** | Vue dev server works in development |
| **Enabled fallback** | SPA routing works correctly |
| **CORS includes ngrok** | Works with ngrok URLs |

---

## If Still Having Issues

### Check 1: Verify Port
```bash
# App should be running on 5274
netstat -ano | findstr :5274
```

### Check 2: Verify Hub Endpoint
Open browser DevTools and check Network tab for:
- Request to `/videocallhub/negotiate`
- Status should be 200
- Response should have `connectionToken`

### Check 3: Check CORS Headers
Response headers should include:
```
Access-Control-Allow-Origin: http://localhost:5274
Access-Control-Allow-Credentials: true
```

---

**All fixes have been applied!** ??

Your SignalR negotiation should now work correctly.
