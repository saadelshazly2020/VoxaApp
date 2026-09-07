# ?? Getting Started - Complete Guide

Welcome to the Video Chat Application! This guide will get you up and running in 5 minutes.

## ? Super Quick Start

### 1?? Double-click to start:
```
start-dev.bat
```

### 2?? Open browser:
```
http://localhost:3000
```

### 3?? Done! ??

---

## ?? Step-by-Step Guide

### Prerequisites Check

Before starting, ensure you have:

1. **Node.js 18+** 
   - Check: `node --version`
   - Download: https://nodejs.org/

2. **.NET 9 SDK**
   - Check: `dotnet --version`
   - Download: https://dotnet.microsoft.com/download

3. **PowerShell** (Already installed on Windows)

---

## ?? Method 1: Automated (Recommended)

### Option A: Batch File (Easiest)
```batch
start-dev.bat
```

### Option B: PowerShell Scripts
```powershell
# Install dependencies (first time only)
.\install-deps.ps1

# Start development servers
.\start-dev.ps1
```

**What happens:**
- ? Installs npm packages (if needed)
- ? Starts .NET backend on port 5000
- ? Starts Vue frontend on port 3000
- ? Opens 2 terminal windows

**Access the app:**
- Frontend: http://localhost:3000
- Backend: http://localhost:5000

---

## ?? Method 2: Manual (For Learning)

### Terminal 1 - Backend Server
```powershell
# Start .NET backend
dotnet run

# Or with auto-reload on file changes:
dotnet watch run
```

**Output should show:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

### Terminal 2 - Frontend Server
```powershell
# Navigate to client app
cd client-app

# Install dependencies (first time only)
npm install

# Start dev server
npm run dev
```

**Output should show:**
```
VITE v5.1.5  ready in 500 ms

?  Local:   http://localhost:3000/
?  press h + enter to show help
```

### Open Browser
Navigate to: **http://localhost:3000**

---

## ?? Test the Application

### Single User Test
1. Open http://localhost:3000
2. Click "Start Video Chat"
3. Enter User ID: `Alice`
4. Click "Register"
5. Allow camera/microphone access
6. ? You should see your video

### Two Users Test
1. **Tab 1**: Register as `Alice`
2. **Tab 2** (or incognito): Register as `Bob`
3. In Alice's tab, you'll see Bob in "Connected Users"
4. Click "Call" button next to Bob
5. ? Both users should see each other's video

### Room Test
1. **Alice**: Enter room ID `meeting-123`, click "Create"
2. **Bob**: Enter room ID `meeting-123`, click "Join"
3. ? Both users in the same room with video

---

## ?? Project Structure Overview

```
VideoChatingApp.WebRTC/
?
??? client-app/              # Vue 3 Frontend
?   ??? src/                 # Source code
?   ?   ??? views/           # Pages (Home, VideoChat, Room)
?   ?   ??? components/      # Reusable components
?   ?   ??? services/        # SignalR, WebRTC
?   ?   ??? models/          # TypeScript types
?   ??? package.json         # NPM dependencies
?   ??? vite.config.ts       # Build configuration
?
??? Core/                    # .NET Business Logic
??? Hubs/                    # SignalR Hub
??? Managers/                # Business Managers
??? wwwroot/                 # Static files
?   ??? dist/                # Built Vue app (after build)
?
??? Program.cs               # .NET Entry point
??? start-dev.bat            # ?? Quick start script
??? start-dev.ps1            # PowerShell dev script
??? build-prod.ps1           # Production build script
??? install-deps.ps1         # Dependency installer
```

---

## ??? Building for Production

### Quick Build
```powershell
.\build-prod.ps1
```

### Manual Build
```powershell
# Build Vue app
cd client-app
npm run build

# Build .NET app
cd ..
dotnet build -c Release
```

### Run Production Build
```powershell
dotnet run --configuration Release
```

Open: **http://localhost:5000**

---

## ?? Making Changes

### Frontend Changes (Vue/TypeScript/CSS)

**Files to edit**: `client-app/src/*`

1. Edit any file in `client-app/src/`
2. Save the file
3. Browser auto-reloads (Hot Module Replacement)
4. No manual refresh needed! ?

**Example**: Edit `client-app/src/views/Home.vue`

### Backend Changes (C#)

**Files to edit**: `Core/*`, `Hubs/*`, `Managers/*`

1. Edit any C# file
2. Save the file
3. If using `dotnet watch run`: Server auto-restarts
4. Otherwise: Stop (Ctrl+C) and restart `dotnet run`

**Example**: Edit `Hubs/VideoCallHub.cs`

---

## ?? Troubleshooting

### ? "Port 3000 already in use"

**Solution 1**: Kill the process
```powershell
# Find process on port 3000
netstat -ano | findstr :3000

# Kill it (replace <PID> with actual number)
taskkill /PID <PID> /F
```

**Solution 2**: Change the port
Edit `client-app/vite.config.ts`:
```typescript
server: { port: 3001 }
```

---

### ? "Cannot connect to SignalR"

**Checklist:**
- ? Is backend running? Check Terminal 1
- ? Is it on port 5000? Look for "Now listening on: http://localhost:5000"
- ? Check browser console (F12) for errors

**Solution**: Restart backend
```powershell
# Stop with Ctrl+C, then:
dotnet run
```

---

### ? "Camera/Microphone not working"

**Reasons:**
1. Browser denied permissions
2. Another app is using camera
3. Not using HTTPS in production

**Solution:**
1. Click browser address bar ? Camera icon ? Allow
2. Close other apps using camera (Zoom, Teams, etc.)
3. For production, use HTTPS (required by browsers)

---

### ? "npm command not found"

**Solution**: Install Node.js
1. Download from https://nodejs.org/
2. Install and restart terminal
3. Verify: `node --version`

---

### ? "dotnet command not found"

**Solution**: Install .NET SDK
1. Download from https://dotnet.microsoft.com/download
2. Install .NET 9 SDK
3. Restart terminal
4. Verify: `dotnet --version`

---

### ? "Build fails with TypeScript errors"

**Solution**: Check for errors
```powershell
cd client-app
npx vue-tsc --noEmit
```

Fix any errors shown, then build again.

---

### ? "Changes not showing in browser"

**Frontend:**
1. Check terminal - should show "HMR update"
2. Hard refresh: `Ctrl + Shift + R`
3. Clear cache: `Ctrl + Shift + Delete`

**Backend:**
1. Use `dotnet watch run` for auto-restart
2. Or manually restart: `Ctrl + C` then `dotnet run`

---

## ?? Next Steps

### 1. Explore the Code
- ?? `client-app/src/views/Home.vue` - Landing page
- ?? `client-app/src/views/VideoChat.vue` - Main chat
- ?? `client-app/src/services/signalr.service.ts` - SignalR
- ?? `client-app/src/services/webrtc.service.ts` - WebRTC
- ?? `Hubs/VideoCallHub.cs` - SignalR hub

### 2. Customize UI
- Edit Tailwind colors in `client-app/tailwind.config.js`
- Modify components in `client-app/src/components/`
- Update layouts in `client-app/src/views/`

### 3. Add Features
- Text chat messages
- Screen sharing
- Recording
- User profiles
- Room management

### 4. Read Documentation
- ?? `client-app/README.md` - Client app guide
- ?? `API.md` - SignalR API documentation
- ?? `ARCHITECTURE.md` - System architecture
- ?? `NEW_STRUCTURE_README.md` - Project structure

---

## ?? Common Tasks

### Install dependencies
```powershell
.\install-deps.ps1
```

### Start development
```powershell
.\start-dev.ps1
# or
start-dev.bat
```

### Build for production
```powershell
.\build-prod.ps1
```

### Run production build
```powershell
dotnet run -c Release
```

### Clean and rebuild
```powershell
# Clean .NET
dotnet clean
rm -r bin, obj

# Clean Node
cd client-app
rm -r node_modules, dist
npm install
```

### Update dependencies
```powershell
# Update .NET packages
dotnet restore

# Update NPM packages
cd client-app
npm update
```

---

## ?? Important Notes

### Development vs Production

**Development**:
- Frontend: http://localhost:3000 (Vite)
- Backend: http://localhost:5000 (separate)
- CORS enabled for localhost:3000
- Hot Module Replacement (HMR)

**Production**:
- Everything: http://localhost:5000 (one server)
- Frontend built into `wwwroot/dist/`
- Served by ASP.NET Core
- No CORS needed

### Browser Requirements
- Chrome 90+
- Firefox 88+
- Edge 90+
- Safari 14+

### HTTPS Requirement
- Development: HTTP is OK
- Production: HTTPS required for camera/mic
- WebRTC needs secure context

---

## ?? Success!

You're all set! Your video chat application is running.

**What to do now:**
1. ? Test with 2 browser tabs
2. ? Try the room feature
3. ? Explore the code
4. ? Customize the UI
5. ? Add new features

**Need help?**
- Check `NEW_STRUCTURE_README.md` for details
- Read `client-app/README.md` for frontend specifics
- See `TROUBLESHOOTING.md` for common issues

---

## ?? Quick Reference Card

```
?? Install:     .\install-deps.ps1
?? Dev Start:   .\start-dev.ps1  or  start-dev.bat
??? Build:       .\build-prod.ps1
?? Run Prod:    dotnet run -c Release
?? Frontend:    http://localhost:3000
?? Backend:     http://localhost:5000
?? Docs:        NEW_STRUCTURE_README.md
```

---

**Happy Coding! ??**

*Built with ?? using Vue 3, TypeScript, Tailwind CSS, and .NET 9*
