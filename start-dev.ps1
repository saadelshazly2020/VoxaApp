# Start Development Environment
# This script starts both the backend (.NET) and frontend (Vue) servers

Write-Host "?? Starting Video Chat Application Development Environment" -ForegroundColor Green
Write-Host ""

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "? Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "? Node.js not found. Please install Node.js 18+ from https://nodejs.org/" -ForegroundColor Red
    exit 1
}

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "? .NET not found. Please install .NET 9 SDK" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "?? Installing client dependencies (if needed)..." -ForegroundColor Cyan
cd client-app
if (!(Test-Path "node_modules")) {
    npm install
} else {
    Write-Host "? Dependencies already installed" -ForegroundColor Green
}
cd ..

Write-Host ""
Write-Host "?? Starting Backend Server (.NET on port 5000/5001)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run"

Write-Host "? Waiting for backend to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "?? Starting Frontend Dev Server (Vue on port 3000)..." -ForegroundColor Cyan
cd client-app
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run dev"

Write-Host ""
Write-Host "? Development environment started!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Access points:" -ForegroundColor Yellow
Write-Host "   Frontend:  http://localhost:3000" -ForegroundColor Cyan
Write-Host "   Backend:   http://localhost:5000" -ForegroundColor Cyan
Write-Host "   SignalR:   http://localhost:5000/videocallhub" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Press Ctrl+C in the other windows to stop the servers" -ForegroundColor Yellow
