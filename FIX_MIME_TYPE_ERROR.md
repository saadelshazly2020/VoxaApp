# Fix for MIME Type Error

## ? Error
```
Failed to load module script: Expected a JavaScript-or-Wasm module script 
but the server responded with a MIME type of "text/html".
```

## ?? Root Cause

The Vue app is trying to load JavaScript modules but receiving HTML instead. This happens when:

1. **SPA Proxy not working** - Browser receives .NET's index.html instead of Vue's
2. **Vue dev server not running** - Proxy has nothing to forward to
3. **CORS misconfigured** - Vue rejects the proxy request
4. **Development mode not enabled** - Production mode serves static files

---

## ? Quick Fix

### Step 1: Stop Everything
```bash
# Press Ctrl+C if app is running
# Close any other instances on ports 3000 and 5274
```

### Step 2: Clean
```bash
cd client-app
rm -r node_modules dist
npm install
```

Or on Windows:
```powershell
cd client-app
Remove-Item -Recurse node_modules, dist
npm install
```

### Step 3: Restart
```bash
# Make sure you're in the root directory
dotnet run
```

---

## ?? What We Fixed

### Program.cs
? **Enabled the SPA proxy middleware** (it was commented out)

```csharp
// Now UNCOMMENTED and ACTIVE
if (app.Environment.IsDevelopment())
{
    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
}
```

? **Added fallback route**

```csharp
// For SPA routing in production or when proxy is used
app.MapFallbackToFile("index.html");
```

### vite.config.ts
? **Enabled CORS**

```typescript
server: {
    port: 3000,
    strictPort: false,  // Allow port change if 3000 in use
    cors: true,         // Enable CORS headers
    proxy: {
        '/videocallhub': {
            target: 'http://localhost:5274',
            ws: true,
            changeOrigin: true
        }
    }
}
```

---

## ?? Verify It's Working

### Option 1: Run Diagnostic
```bash
.\diagnose-mime-error.ps1
```

### Option 2: Manual Test

1. **Start the app:**
   ```bash
   dotnet run
   ```

2. **Open browser:** `http://localhost:5274`

3. **Check console:**
   - Press F12 ? Console
   - Should see Vue app loading, NOT module errors

4. **Verify Network tab:**
   - F12 ? Network
   - Should see requests proxied correctly
   - All .js files should have `application/javascript` MIME type

---

## ?? What to Look For

### ? Working
- Browser shows Vue app
- F12 Console has no MIME type errors
- Network tab shows .js files with correct MIME type
- Hot reload works (save changes, see updates instantly)

### ? Not Working
- Browser blank or shows error
- Console shows "Failed to load module script"
- Network tab shows .html responses for .js requests
- Multiple errors in console

---

## ?? Advanced Troubleshooting

### Check if Vue is running
```bash
curl http://localhost:3000
# Should get HTML response, not error
```

### Check if proxy works
```bash
curl http://localhost:5274/
# Should get Vue app from localhost:3000
```

### Check CORS
```bash
curl -H "Origin: http://localhost:5274" http://localhost:3000 -v
# Should see Access-Control-Allow-Origin header
```

### Check Development mode
Add logging to Program.cs:
```csharp
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("? Running in Development mode - SPA proxy ENABLED");
    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
}
else
{
    Console.WriteLine("? Running in Production mode - Using static files");
}
```

---

## ?? Configuration Summary

### Program.cs - Key Settings
```csharp
// 1. CORS allows communication between ports
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5274"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// 2. SPA static files service
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "client-app/dist";
});

// 3. SPA proxy middleware
app.UseSpa(spa =>
{
    spa.Options.SourcePath = "client-app";

    if (app.Environment.IsDevelopment())
    {
        // CRITICAL: This proxies requests to Vue
        spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
    }
});

// 4. Fallback for SPA routing
app.MapFallbackToFile("index.html");
```

### vite.config.ts - Key Settings
```typescript
server: {
    port: 3000,
    strictPort: false,      // Fallback to next port if busy
    cors: true,             // Enable CORS responses
    proxy: {
        '/videocallhub': {  // WebSocket proxy
            target: 'http://localhost:5274',
            ws: true,
            changeOrigin: true
        }
    }
}
```

---

## ?? Flow After Fix

```
Browser (localhost:5274)
    ?
.NET SPA Proxy
    ?
Vue Dev Server (localhost:3000)
    ?
Returns modules with correct MIME type
    ?
Browser loads Vue with hot reload ?
```

---

## ?? Next Steps

1. **Apply the fixes** (already done in your files)
2. **Stop any running processes** (Ctrl+C)
3. **Run:** `dotnet run`
4. **Open:** `http://localhost:5274`
5. **Check console** for no errors
6. **Make a change** and watch it update instantly!

---

## ?? Still Having Issues?

Run the diagnostic:
```bash
.\diagnose-mime-error.ps1
```

This will:
- ? Check if Vue is running
- ? Check if .NET is running
- ? Check proxy configuration
- ? Test module access
- ? Verify CORS headers

---

**Your setup should now work perfectly!** ??
