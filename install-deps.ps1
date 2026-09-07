# Install Dependencies
# This script installs all required dependencies for both client and server

Write-Host "?? Installing Video Chat Application Dependencies" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "?? Checking prerequisites..." -ForegroundColor Cyan

try {
    $nodeVersion = node --version
    Write-Host "? Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "? Node.js not found. Please install from https://nodejs.org/" -ForegroundColor Red
    exit 1
}

try {
    $npmVersion = npm --version
    Write-Host "? npm: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "? npm not found!" -ForegroundColor Red
    exit 1
}

try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "? .NET SDK not found. Please install .NET 9 SDK" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "?? Installing Client Dependencies..." -ForegroundColor Cyan
cd client-app

if (Test-Path "package-lock.json") {
    Write-Host "   Found package-lock.json, running npm ci..." -ForegroundColor Gray
    npm ci
} else {
    Write-Host "   Running npm install..." -ForegroundColor Gray
    npm install
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Client dependency installation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? Client dependencies installed!" -ForegroundColor Green
cd ..

Write-Host ""
Write-Host "?? Restoring .NET Dependencies..." -ForegroundColor Cyan
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "? .NET restore failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? .NET dependencies restored!" -ForegroundColor Green

Write-Host ""
Write-Host "?? All dependencies installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Next steps:" -ForegroundColor Yellow
Write-Host "   To start development: .\start-dev.ps1" -ForegroundColor Cyan
Write-Host "   To build production:  .\build-prod.ps1" -ForegroundColor Cyan
