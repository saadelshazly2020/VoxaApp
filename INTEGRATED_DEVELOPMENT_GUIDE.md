# Integrated .NET + Vue Development Guide

## Overview

Your .NET application is now configured to automatically manage the Vue development server. This guide explains how everything works and how to use it.

## ?? Quick Start

### Option 1: Simple Start (Recommended)
Just run the .NET app - it will automatically start the Vue dev server:

```bash
dotnet run
```

Or from Visual Studio: Press F5 or click "Start Debugging"

### Option 2: Using Scripts

```bash
# Windows PowerShell
.\start-dev.ps1

# Windows Command Prompt
start-dev.bat
```

## ?? What Happens When You Run `dotnet run`

1. **Node.js Check**: Verifies Node.js is installed
2. **NPM Install**: Automatically installs npm packages if `node_modules` doesn't exist
3. **.NET Build**: Compiles the .NET application
4. **Vue Dev Server**: Starts on port 3000 (automatically)
5. **.NET App**: Starts on port 5274 and proxies UI requests to Vue

## ?? URLs

| Service | URL | Purpose |
|---------|-----|---------|
| **Main App** | `http://localhost:5274` | ? **Use this URL** - Full integrated app |
| Vue Dev Server | `http://localhost:3000` | Direct Vue access (optional) |
| SignalR Hub | `http://localhost:5274/videocallhub` | WebSocket endpoint |

## ??? How It Works

### Development Mode

```
Browser Request ? .NET App (5274)
                    ?
    ?????????????????????????????????
    ?                               ?
SignalR Hub                    SPA Proxy
    ?                               ?
    ????????????????  Vue Dev Server (3000)
                      Hot Module Reload
```

**Features:**
- ?? **Hot Module Reload**: Vue changes reflect immediately
- ?? **Auto Restart**: .NET changes require manual restart
- ?? **No CORS Issues**: Everything goes through one port
- ?? **Automatic npm install**: Dependencies installed when needed

### Production Mode

```
Browser Request ? .NET App (5274)
                    ?
    ?????????????????????????????????
    ?                               ?
SignalR Hub              Static Files (wwwroot/dist)
```

**Features:**
- ?? **Automatic Build**: Vue app built during `dotnet publish`
- ?? **Optimized**: Minified and bundled files
- ?? **Single Server**: Everything served from .NET

## ??? Configuration Files

### VideoChatingApp.WebRTC.csproj
Configures SPA integration:

```xml
<SpaRoot>client-app\</SpaRoot>
<SpaProxyServerUrl>http://localhost:3000</SpaProxyServerUrl>
<SpaProxyLaunchCommand>npm run dev</SpaProxyLaunchCommand>
```

### Program.cs
Sets up SPA middleware:

```csharp
// Development: Proxy to Vue dev server
spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");

// Production: Serve static files
app.UseSpaStaticFiles();
```

### client-app/vite.config.ts
Configures Vite to work with .NET:

```typescript
server: {
    port: 3000,
    strictPort: true,
    cors: true,
    proxy: {
        '/videocallhub': {
            target: 'http://localhost:5274',
            ws: true,
            changeOrigin: true
        }
    }
}
```

## ?? Building for Production

### Build Everything

```bash
# PowerShell script that builds both Vue and .NET
.\build-prod.ps1
```

Or manually:

```bash
# Step 1: Build Vue app
cd client-app
npm run build

# Step 2: Build .NET app
cd ..
dotnet build -c Release
```

### Publish for Deployment

```bash
dotnet publish -c Release -o ./publish
```

This will:
1. Build the Vue app (`npm run build`)
2. Copy files to `wwwroot/dist`
3. Include everything in the publish output

## ?? Testing

### Local Testing

```bash
# Development mode
dotnet run

# Production mode
dotnet run --configuration Release
```

### With ngrok

```bash
# Terminal 1: Start the app
dotnet run

# Terminal 2: Expose via ngrok
ngrok http 5274
```

Access your app:
- **Laptop**: `http://localhost:5274`
- **Mobile**: `https://your-ngrok-url.ngrok-free.app`

## ?? Troubleshooting

### Issue: Vue dev server doesn't start

**Symptoms:**
- Browser shows "Cannot connect to development server"
- Console error about port 3000

**Solutions:**
1. **Check Node.js**: `node --version` (should be v18 or higher)
2. **Install dependencies**:
   ```bash
   cd client-app
   npm install
   ```
3. **Check port availability**: Make sure port 3000 is not in use
4. **Manual start**: Try starting Vue manually:
   ```bash
   cd client-app
   npm run dev
   ```

### Issue: Changes not reflecting

**Vue changes:**
- Should reflect immediately (Hot Module Reload)
- If not, check browser console for errors
- Try refreshing the page

**.NET changes:**
- Stop the app (Ctrl+C or Stop in VS)
- Restart: `dotnet run`

**Config changes:**
- Restart both .NET and Vue
- In some cases, clean and rebuild:
  ```bash
  dotnet clean
  dotnet build
  ```

### Issue: Build fails

**Error: "Node.js is required"**
- Install Node.js from https://nodejs.org/

**Error: "npm not found"**
- Restart your terminal/IDE after installing Node.js

**Error: Port 3000 or 5274 already in use**
- Close other instances
- Or change ports in `launchSettings.json` and `vite.config.ts`

### Issue: CORS errors

This shouldn't happen with the integrated setup, but if it does:

1. **Check CORS policy** in `Program.cs`:
   ```csharp
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
   ```

2. **Restart both servers**

## ?? File Structure

```
VideoChatingApp.WebRTC/
??? client-app/                # Vue application
?   ??? src/
?   ??? package.json
?   ??? vite.config.ts
?   ??? node_modules/         # Auto-installed
??? wwwroot/
?   ??? dist/                 # Vue build output (auto-generated)
??? Program.cs                # .NET entry point with SPA config
??? VideoChatingApp.WebRTC.csproj  # Project file with SPA integration
??? Properties/
    ??? launchSettings.json   # Launch configuration
```

## ?? Best Practices

### Development Workflow

1. **Start the app**: `dotnet run`
2. **Open browser**: `http://localhost:5274`
3. **Make changes**:
   - Vue files: Save ? See changes immediately
   - .NET files: Save ? Stop app ? Restart
4. **Test**: Use the integrated URL (`localhost:5274`)

### Before Committing

```bash
# Run build to ensure everything works
.\build-prod.ps1

# Run tests (if you have them)
dotnet test
```

### Deployment Checklist

- [ ] Run `dotnet publish -c Release`
- [ ] Test the published version locally
- [ ] Verify `wwwroot/dist` contains Vue files
- [ ] Check environment variables in production
- [ ] Test ngrok or actual deployment URL

## ?? Adding New Features

### Vue Components

1. Create component in `client-app/src/components/`
2. Save file
3. Changes appear immediately in browser

### .NET Endpoints

1. Add endpoint/controller
2. Save file
3. Stop and restart `dotnet run`
4. Test endpoint

### Environment Variables

**Development** (`appsettings.Development.json`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Production** (`appsettings.json` or environment):
```json
{
  "AllowedHosts": "yourdomain.com"
}
```

## ?? Security Notes

### Development
- CORS is open for localhost
- All origins on localhost are trusted
- SignalR allows any method/header

### Production
**?? Update CORS policy:**

```csharp
options.AddPolicy("Production", policy =>
{
    policy.WithOrigins("https://yourdomain.com")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
```

## ?? Additional Resources

- [ASP.NET Core SPA Documentation](https://learn.microsoft.com/en-us/aspnet/core/client-side/spa-services)
- [Vite Documentation](https://vitejs.dev/)
- [Vue 3 Documentation](https://vuejs.org/)

## ? Benefits of This Setup

1. **Single Command**: Just run `dotnet run`
2. **No CORS Issues**: Everything through one URL
3. **Hot Reload**: Vue changes reflect immediately
4. **Production Ready**: Same code works in production
5. **Automatic Build**: Vue builds during publish
6. **IDE Friendly**: Works with Visual Studio, VS Code, Rider
7. **Simple Debugging**: Debug both .NET and Vue simultaneously

## ?? Summary

You now have a fully integrated development environment where:

- ? Running `dotnet run` starts everything
- ? Vue hot reload works automatically
- ? No CORS configuration needed
- ? Production builds are automatic
- ? Single URL for development

**Just run `dotnet run` and start coding!** ??
