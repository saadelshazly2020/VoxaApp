# Vue 3 Video Chat Client Application

This is the frontend client application for the Video Chat system, built with Vue 3, TypeScript, and Tailwind CSS.

## ?? Folder Structure

```
client-app/
??? src/
?   ??? assets/
?   ?   ??? styles/
?   ?       ??? main.css           # Tailwind CSS + custom styles
?   ??? components/
?   ?   ??? AppHeader.vue          # Navigation header
?   ?   ??? UserList.vue           # Connected users list
?   ?   ??? VideoGrid.vue          # Video grid layout
?   ??? models/
?   ?   ??? user.model.ts          # User type definitions
?   ?   ??? room.model.ts          # Room type definitions
?   ?   ??? webrtc.model.ts        # WebRTC type definitions
?   ??? services/
?   ?   ??? signalr.service.ts     # SignalR connection
?   ?   ??? webrtc.service.ts      # WebRTC peer management
?   ??? views/
?   ?   ??? Home.vue               # Landing page (/)
?   ?   ??? VideoChat.vue          # Main video chat (/video-chat)
?   ?   ??? Room.vue               # Specific room (/room/:id)
?   ??? App.vue                    # Root component
?   ??? main.ts                    # Application entry point
??? index.html                     # HTML template
??? package.json                   # NPM dependencies
??? vite.config.ts                 # Vite configuration
??? tsconfig.json                  # TypeScript configuration
??? tailwind.config.js             # Tailwind CSS configuration
??? postcss.config.js              # PostCSS configuration
```

## ?? Quick Start

### Install Dependencies
```bash
cd client-app
npm install
```

### Development Mode
```bash
npm run dev
```
Opens at `http://localhost:3000` with hot-reload.

### Build for Production
```bash
npm run build
```
Outputs to `../wwwroot/dist/`

### Preview Production Build
```bash
npm run preview
```

## ?? Configuration

### Vite Configuration (`vite.config.ts`)
- **Dev Server**: Port 3000
- **Proxy**: `/videocallhub` ? `http://localhost:5000` (SignalR)
- **Build Output**: `../wwwroot/dist/`
- **Code Splitting**: Vendor chunks (Vue, SignalR)

### TypeScript Configuration
- Strict mode enabled
- No unused locals/parameters
- Path alias: `@/` ? `./src/`

### Tailwind CSS
- Custom primary color palette (blue)
- Responsive utilities
- Custom component classes (btn, card, badge, input)

## ?? Dependencies

### Production
- **vue**: ^3.4.21 - Progressive JavaScript framework
- **vue-router**: ^4.3.0 - Official routing library
- **@microsoft/signalr**: ^8.0.0 - Real-time communication

### Development
- **vite**: ^5.1.5 - Fast build tool
- **typescript**: ^5.4.2 - Type safety
- **tailwindcss**: ^3.4.1 - Utility-first CSS
- **vue-tsc**: ^2.0.6 - TypeScript type checking for Vue

## ?? Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/` | Home.vue | Landing page with action cards |
| `/video-chat` | VideoChat.vue | Main video chat interface |
| `/room/:roomId` | Room.vue | Direct room access |

## ?? UI Components

### AppHeader.vue
- Shows connection status (green/red indicator)
- Displays current user info
- Navigation to home

### UserList.vue
- Lists all connected users
- Shows user status (available/in room)
- Call buttons for 1-to-1 calls

### VideoGrid.vue
- Responsive video layout (1-9+ videos)
- Automatic grid sizing
- Local and remote video streams
- Video labels with user names

## ?? Services

### SignalRService
- WebSocket connection to backend
- Event-based architecture
- Automatic reconnection
- Methods: register, createRoom, joinRoom, leaveRoom, sendOffer, sendAnswer, sendIceCandidate

### WebRTCService
- Peer-to-peer connections
- Media stream management
- ICE candidate handling
- Audio/video toggle controls

## ??? Available Scripts

```bash
# Development with hot-reload
npm run dev

# Type check only (no build)
npx vue-tsc --noEmit

# Build for production
npm run build

# Preview production build locally
npm run preview

# Install dependencies
npm install

# Clean install
rm -rf node_modules package-lock.json && npm install
```

## ?? Integration with .NET Backend

### Development Mode
1. Start .NET backend: `dotnet run` (port 5000/5001)
2. Start Vue dev server: `cd client-app && npm run dev` (port 3000)
3. Vite proxies SignalR requests to backend

### Production Mode
1. Build Vue app: `cd client-app && npm run build`
2. Files are output to `wwwroot/dist/`
3. .NET Core serves the built files
4. Run: `dotnet run`

## ?? Build Output

After `npm run build`:

```
../wwwroot/dist/
??? index.html
??? assets/
    ??? index-[hash].css        (~12KB gzipped)
    ??? vendor-[hash].js        (~50KB gzipped)
    ??? signalr-[hash].js       (~15KB gzipped)
    ??? index-[hash].js         (~20KB gzipped)
```

## ?? Troubleshooting

### Port 3000 already in use
```bash
# Change port in vite.config.ts
server: { port: 3001 }
```

### Backend connection fails
- Ensure backend is running on port 5000/5001
- Check proxy configuration in `vite.config.ts`
- Verify CORS settings in backend `Program.cs`

### TypeScript errors
```bash
# Run type check
npx vue-tsc --noEmit

# Check specific file
npx vue-tsc --noEmit src/views/Home.vue
```

### Build errors
```bash
# Clear cache and rebuild
rm -rf node_modules dist
npm install
npm run build
```

## ?? Development Workflow

1. **Start Backend**: `dotnet run` in project root
2. **Start Frontend**: `cd client-app && npm run dev`
3. **Make Changes**: Edit files in `src/`
4. **See Changes**: Browser auto-reloads
5. **Build**: `npm run build` when ready
6. **Test Production**: `npm run preview`

## ?? Deployment

### Option 1: Serve from ASP.NET Core
```bash
cd client-app
npm run build
cd ..
dotnet publish -c Release
```

### Option 2: Static CDN + API Backend
```bash
cd client-app
npm run build
# Upload wwwroot/dist/* to CDN
# Configure reverse proxy for /videocallhub
```

## ?? Environment Variables

Create `.env.local` for environment-specific configs:

```env
VITE_API_URL=http://localhost:5000
VITE_SIGNALR_HUB=/videocallhub
```

Usage in code:
```typescript
const apiUrl = import.meta.env.VITE_API_URL;
```

## ?? Security Best Practices

- ? HTTPS in production (required for WebRTC)
- ? Implement authentication
- ? Validate all user inputs
- ? Sanitize data before rendering
- ? Use environment variables for sensitive configs
- ? Enable CORS only for trusted domains

## ?? Additional Resources

- [Vue 3 Documentation](https://vuejs.org/)
- [Vite Documentation](https://vitejs.dev/)
- [Tailwind CSS](https://tailwindcss.com/)
- [WebRTC API](https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)

---

**This client app is part of the VideoChatingApp.WebRTC solution.**
