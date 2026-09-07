#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Diagnose and fix the MIME type error for module scripts
.DESCRIPTION
    Checks the SPA configuration and provides fixes
#>

Write-Host "?? Diagnosing MIME Type Error" -ForegroundColor Cyan
Write-Host ""

# Check 1: Vue dev server is running
Write-Host "? Checking Vue dev server..." -NoNewline
$vueRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000" -TimeoutSec 2 -ErrorAction SilentlyContinue
    $vueRunning = $true
    Write-Host " RUNNING" -ForegroundColor Green
} catch {
    Write-Host " NOT RUNNING" -ForegroundColor Yellow
}

# Check 2: .NET app is running
Write-Host "? Checking .NET app..." -NoNewline
$dotnetRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5274" -TimeoutSec 2 -ErrorAction SilentlyContinue
    $dotnetRunning = $true
    Write-Host " RUNNING" -ForegroundColor Green
} catch {
    Write-Host " NOT RUNNING" -ForegroundColor Yellow
}

# Check 3: Check Program.cs configuration
Write-Host "? Checking Program.cs SPA configuration..." -NoNewline
$programContent = Get-Content "Program.cs" -Raw
if ($programContent -match "UseProxyToSpaDevelopmentServer") {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " MISSING PROXY" -ForegroundColor Red
}

# Check 4: Verify Vue is accessible
Write-Host "? Testing Vue module access..." -NoNewline
if ($vueRunning) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:3000/src/main.ts" -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host " WORKING" -ForegroundColor Green
        } else {
            Write-Host " WRONG RESPONSE" -ForegroundColor Yellow
        }
    } catch {
        Write-Host " UNREACHABLE" -ForegroundColor Yellow
    }
} else {
    Write-Host " SKIPPED (Vue not running)" -ForegroundColor Gray
}

# Check 5: Verify proxy configuration
Write-Host "? Checking CORS headers..." -NoNewline
if ($vueRunning) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:3000" -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.Headers."Access-Control-Allow-Origin" -or $response.Headers."access-control-allow-origin") {
            Write-Host " ENABLED" -ForegroundColor Green
        } else {
            Write-Host " DISABLED" -ForegroundColor Yellow
        }
    } catch {
        Write-Host " CHECK FAILED" -ForegroundColor Yellow
    }
} else {
    Write-Host " SKIPPED (Vue not running)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Gray

# Provide solutions
Write-Host ""
if (!$dotnetRunning -and !$vueRunning) {
    Write-Host "? Both servers are down!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Solution:" -ForegroundColor Yellow
    Write-Host "  1. Stop any running processes" -ForegroundColor White
    Write-Host "  2. Run: dotnet run" -ForegroundColor Cyan
} elseif (!$vueRunning) {
    Write-Host "??  Vue dev server is not running" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Solution:" -ForegroundColor Yellow
    Write-Host "  The proxy won't work without Vue running." -ForegroundColor White
    Write-Host "  Run: dotnet run" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "This should auto-start Vue on port 3000" -ForegroundColor Gray
} else {
    Write-Host "??  Servers are running" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Clear browser cache (Ctrl+Shift+Delete)" -ForegroundColor White
    Write-Host "2. Hard refresh (Ctrl+Shift+R)" -ForegroundColor White
    Write-Host "3. Check browser console for errors" -ForegroundColor White
    Write-Host "4. Verify Vue app loaded at localhost:3000" -ForegroundColor White
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Gray

# Show what should happen
Write-Host ""
Write-Host "?? Expected flow:" -ForegroundColor Cyan
Write-Host "  1. Browser requests http://localhost:5274/" -ForegroundColor Gray
Write-Host "  2. .NET receives request" -ForegroundColor Gray
Write-Host "  3. .NET proxies to http://localhost:3000/" -ForegroundColor Gray
Write-Host "  4. Vue dev server responds" -ForegroundColor Gray
Write-Host "  5. Browser loads Vue app with hot reload" -ForegroundColor Gray
Write-Host ""
