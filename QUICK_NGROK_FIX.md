# ? QUICK FIX - ngrok WSS Error

## Problem
```
URL scheme "wss" is not supported
```

## Why
You were using full URLs with ngrok. This breaks because ngrok is a reverse proxy and needs to handle the HTTP negotiation first.

## Solution
Use **relative URLs instead** (they work everywhere):

```typescript
// ? FIXED
return '/videocallhub';

// ? OLD (broken with ngrok)
return `${protocol}://${host}/videocallhub`;
```

## What Changed

1. **config.ts** - Now always uses relative URLs
2. **Program.cs** - Fixed all configuration issues

## Test It

```bash
# 1. Stop app (Ctrl+C)

# 2. Rebuild
dotnet clean && dotnet build

# 3. Start
dotnet run

# 4. Test
# Localhost: http://localhost:5274 ?
# ngrok: https://4e97-194-238-97-224.ngrok-free.app ?
```

## Expected Results

? No "wss is not supported" error
? Can register users
? User list appears
? Works on both localhost AND ngrok

---

**Done! Both localhost and ngrok will work now.** ??
