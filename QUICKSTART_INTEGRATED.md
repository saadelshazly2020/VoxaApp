# Quick Start - Integrated .NET + Vue

## ?? Start Development

**Just one command:**

```bash
dotnet run
```

**Or use the script:**

```bash
.\start-integrated.ps1
```

**Then open:** `http://localhost:5274`

---

## ? What You Get

- ? .NET app automatically starts Vue dev server
- ? Hot reload for Vue changes
- ? No CORS issues
- ? Single URL for everything
- ? Automatic npm install

---

## ?? Common Tasks

### Development

```bash
# Start everything
dotnet run

# Clean and rebuild
dotnet clean
dotnet build
```

### Build for Production

```bash
# Build script (builds both Vue and .NET)
.\build-prod.ps1

# Or manually
cd client-app
npm run build
cd ..
dotnet build -c Release
```

### Publish

```bash
dotnet publish -c Release -o ./publish
```

---

## ?? URLs

| What | URL |
|------|-----|
| **Main App** | `http://localhost:5274` ? |
| Vue Dev | `http://localhost:3000` |
| SignalR | `http://localhost:5274/videocallhub` |

---

## ?? Troubleshooting

### Vue not starting?

```bash
cd client-app
npm install
npm run dev
```

### Port busy?

- Close other instances
- Check Task Manager for node/dotnet processes

### Changes not showing?

- **Vue**: Should be instant (refresh if needed)
- **.NET**: Stop and restart `dotnet run`

---

## ?? Full Guide

See `INTEGRATED_DEVELOPMENT_GUIDE.md` for complete documentation.

---

## ?? Benefits

? **Simple** - One command to start
? **Fast** - Hot reload for Vue
? **Reliable** - No CORS issues
? **Production Ready** - Same code in prod

**Happy coding!** ??
