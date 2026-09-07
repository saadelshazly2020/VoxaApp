# Vue 3 Video Chat Application

A modern, real-time video chat application built with Vue 3, TypeScript, Tailwind CSS, and WebRTC.

## ?? Features

- ? **Vue 3 Composition API** with TypeScript
- ? **Tailwind CSS** for modern, responsive UI
- ? **Vue Router** for navigation
- ? **Real-time communication** with SignalR
- ? **WebRTC** peer-to-peer video/audio
- ? **Multiple room support**
- ? **1-to-1 and group calls**
- ? **Mute/unmute controls**
- ? **Video on/off controls**

## ?? Prerequisites

- Node.js 18+ and npm/yarn
- .NET 9 SDK (for backend)
- Modern browser (Chrome 90+, Firefox 88+, Edge 90+)

## ??? Installation

### 1. Install Dependencies

```bash
npm install
```

### 2. Start Backend Server

In a separate terminal, navigate to your .NET project directory:

```bash
cd D:\POC\VideoChatingApp.WebRTC
dotnet run
```

The backend should be running on `http://localhost:5000` and `https://localhost:5001`.

### 3. Start Development Server

```bash
npm run dev
```

The Vue app will start on `http://localhost:3000` with hot-reload.

## ?? Usage

### Open the Application

1. Open your browser to `http://localhost:3000`
2. You'll see the Home page with two options:
   - **Start Video Chat** - Join the main chat room
   - **Join a Room** - Enter a specific room ID

### Start a Video Chat

1. Click "Start Video Chat"
2. Enter your User ID (e.g., "Alice")
3. Click "Register"
4. Allow camera/microphone access
5. You'll see your video in the main area

### Call Another User (1-to-1)

1. Open a second browser tab (or use incognito mode)
2. Go to `http://localhost:3000`
3. Click "Start Video Chat"
4. Enter a different User ID (e.g., "Bob")
5. Click "Register"
6. In Alice's view, you'll see Bob in the "Connected Users" list
7. Click "Call" button next to Bob
8. Both users will see each other's video

### Join a Room (Group Call)

1. In Alice's view, enter a Room ID (e.g., "meeting-123")
2. Click "Create" to create the room
3. In Bob's view, enter the same Room ID
4. Click "Join"
5. Both users are now in the same room with video

### Direct Room Link

You can also join a room directly via URL:

```
http://localhost:3000/room/meeting-123
```

## ??? Project Structure

```
.
??? src/
?   ??? assets/
?   ?   ??? styles/
?   ?       ??? main.css          # Tailwind CSS + custom styles
?   ??? components/
?   ?   ??? AppHeader.vue         # Navigation header
?   ?   ??? UserList.vue          # Connected users list
?   ?   ??? VideoGrid.vue         # Video grid layout
?   ??? models/
?   ?   ??? user.model.ts         # User type definitions
?   ?   ??? room.model.ts         # Room type definitions
?   ?   ??? webrtc.model.ts       # WebRTC type definitions
?   ??? services/
?   ?   ??? signalr.service.ts    # SignalR connection
?   ?   ??? webrtc.service.ts     # WebRTC peer management
?   ??? views/
?   ?   ??? Home.vue              # Landing page
?   ?   ??? VideoChat.vue         # Main video chat page
?   ?   ??? Room.vue              # Specific room page
?   ??? App.vue                   # Root component
?   ??? main.ts                   # Application entry
??? index.html                    # HTML template
??? vite.config.ts                # Vite configuration
??? tailwind.config.js            # Tailwind CSS config
??? tsconfig.json                 # TypeScript config
??? package.json                  # Dependencies
```

## ?? UI Components

### Home Page
- Hero section with application overview
- Two action cards: "Start Video Chat" and "Join a Room"
- Features section highlighting key capabilities

### Video Chat Page
- Responsive sidebar with user registration and room controls
- Main video grid area with automatic layout
- Connected users list with call buttons
- Media controls (mute/unmute, video on/off)
- Room management (create/join/leave)

### Room Page
- Direct room access via URL
- Registration form for new users
- Full-screen video grid
- Media controls

## ?? Configuration

### Backend Proxy

The Vite dev server is configured to proxy SignalR requests to the backend:

```typescript
// vite.config.ts
server: {
  port: 3000,
  proxy: {
    '/videocallhub': {
      target: 'http://localhost:5000',
      ws: true,
      changeOrigin: true
    }
  }
}
```

### Tailwind CSS

Custom theme colors and components are defined in `tailwind.config.js` and `src/assets/styles/main.css`.

## ?? Build for Production

### 1. Build the Vue App

```bash
npm run build
```

This will create optimized files in `../wwwroot/dist`.

### 2. Update ASP.NET Core

Modify your `Program.cs` to serve the built files:

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("dist/index.html");
```

### 3. Run Production Build

```bash
cd D:\POC\VideoChatingApp.WebRTC
dotnet run --configuration Release
```

## ?? Responsive Design

The UI is fully responsive and works on:
- Desktop (1920x1080+)
- Laptop (1366x768+)
- Tablet (768x1024)
- Mobile (360x640+)

## ?? Troubleshooting

### Issue: "Cannot connect to SignalR"
- Ensure backend is running on port 5000/5001
- Check browser console for connection errors
- Verify proxy configuration in `vite.config.ts`

### Issue: "Camera/Microphone not accessible"
- Allow permissions in browser
- Use HTTPS in production (required by browsers)
- Check if another app is using the camera

### Issue: "Build fails"
- Delete `node_modules` and run `npm install` again
- Clear Vite cache: `npm run dev -- --force`

## ?? Security Notes

- Use HTTPS in production
- Implement authentication for production use
- Add CORS configuration for production domains
- Consider TURN servers for NAT traversal

## ?? Technologies Used

- **Vue 3** - Progressive JavaScript framework
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool
- **Tailwind CSS** - Utility-first CSS
- **Vue Router** - Official routing
- **SignalR** - Real-time communication
- **WebRTC** - Peer-to-peer video/audio

## ?? License

MIT License - See LICENSE file for details

## ?? Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## ?? Support

For issues and questions, please open an issue on GitHub.

---

**Built with ?? using Vue 3 and .NET 9**
