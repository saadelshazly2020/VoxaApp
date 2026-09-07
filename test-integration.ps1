#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tests the integrated .NET + Vue setup
.DESCRIPTION
    Verifies that all components are properly configured
#>

Write-Host "?? Testing Integrated Setup..." -ForegroundColor Cyan
Write-Host ""

$errors = @()
$warnings = @()

# Test 1: Check Node.js
Write-Host "? Checking Node.js..." -NoNewline
try {
    $nodeVersion = node --version 2>$null
    if ($nodeVersion) {
        Write-Host " OK ($nodeVersion)" -ForegroundColor Green
    } else {
        $errors += "Node.js is not installed"
        Write-Host " FAILED" -ForegroundColor Red
    }
} catch {
    $errors += "Node.js is not installed"
    Write-Host " FAILED" -ForegroundColor Red
}

# Test 2: Check npm
Write-Host "? Checking npm..." -NoNewline
try {
    $npmVersion = npm --version 2>$null
    if ($npmVersion) {
        Write-Host " OK ($npmVersion)" -ForegroundColor Green
    } else {
        $errors += "npm is not installed"
        Write-Host " FAILED" -ForegroundColor Red
    }
} catch {
    $errors += "npm is not installed"
    Write-Host " FAILED" -ForegroundColor Red
}

# Test 3: Check client-app directory
Write-Host "? Checking client-app directory..." -NoNewline
if (Test-Path "client-app") {
    Write-Host " OK" -ForegroundColor Green
} else {
    $errors += "client-app directory not found"
    Write-Host " FAILED" -ForegroundColor Red
}

# Test 4: Check package.json
Write-Host "? Checking package.json..." -NoNewline
if (Test-Path "client-app\package.json") {
    Write-Host " OK" -ForegroundColor Green
} else {
    $errors += "client-app\package.json not found"
    Write-Host " FAILED" -ForegroundColor Red
}

# Test 5: Check vite.config.ts
Write-Host "? Checking vite.config.ts..." -NoNewline
if (Test-Path "client-app\vite.config.ts") {
    Write-Host " OK" -ForegroundColor Green
} else {
    $warnings += "client-app\vite.config.ts not found"
    Write-Host " WARNING" -ForegroundColor Yellow
}

# Test 6: Check .csproj file
Write-Host "? Checking VideoChatingApp.WebRTC.csproj..." -NoNewline
if (Test-Path "VideoChatingApp.WebRTC.csproj") {
    $csprojContent = Get-Content "VideoChatingApp.WebRTC.csproj" -Raw
    
    $hasSpaRoot = $csprojContent -match '<SpaRoot>'
    $hasSpaProxy = $csprojContent -match 'Microsoft.AspNetCore.SpaServices.Extensions'
    
    if ($hasSpaRoot -and $hasSpaProxy) {
        Write-Host " OK (SPA configured)" -ForegroundColor Green
    } else {
        if (!$hasSpaRoot) {
            $warnings += ".csproj missing SpaRoot configuration"
        }
        if (!$hasSpaProxy) {
            $warnings += ".csproj missing SpaServices.Extensions package"
        }
        Write-Host " WARNING (SPA config incomplete)" -ForegroundColor Yellow
    }
} else {
    $errors += "VideoChatingApp.WebRTC.csproj not found"
    Write-Host " FAILED" -ForegroundColor Red
}

# Test 7: Check Program.cs
Write-Host "? Checking Program.cs..." -NoNewline
if (Test-Path "Program.cs") {
    $programContent = Get-Content "Program.cs" -Raw
    
    $hasSpaConfig = $programContent -match 'UseSpa'
    
    if ($hasSpaConfig) {
        Write-Host " OK (SPA middleware configured)" -ForegroundColor Green
    } else {
        $warnings += "Program.cs missing UseSpa configuration"
        Write-Host " WARNING (SPA middleware not configured)" -ForegroundColor Yellow
    }
} else {
    $errors += "Program.cs not found"
    Write-Host " FAILED" -ForegroundColor Red
}

# Test 8: Check if node_modules exists
Write-Host "? Checking node_modules..." -NoNewline
if (Test-Path "client-app\node_modules") {
    Write-Host " OK (dependencies installed)" -ForegroundColor Green
} else {
    Write-Host " Not installed (will auto-install on first run)" -ForegroundColor Yellow
    $warnings += "Run 'cd client-app && npm install' or 'dotnet run' to install dependencies"
}

# Test 9: Check wwwroot directory
Write-Host "? Checking wwwroot directory..." -NoNewline
if (Test-Path "wwwroot") {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " Created" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path "wwwroot" -Force | Out-Null
}

# Test 10: Try building
Write-Host "? Testing build..." -NoNewline
$buildOutput = dotnet build --no-restore 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host " OK" -ForegroundColor Green
} else {
    $errors += "Build failed"
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Build output:" -ForegroundColor Gray
    Write-Host $buildOutput -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Gray

if ($errors.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "? All tests passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Ready to start development!" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Run: dotnet run" -ForegroundColor White
    Write-Host "Or:  .\start-integrated.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "Then open: http://localhost:5274" -ForegroundColor Yellow
} else {
    if ($errors.Count -gt 0) {
        Write-Host "? Errors found:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        Write-Host ""
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host "??  Warnings:" -ForegroundColor Yellow
        $warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
        Write-Host ""
    }
    
    if ($errors.Count -eq 0) {
        Write-Host "Setup is functional but has warnings." -ForegroundColor Yellow
        Write-Host "You can still run: dotnet run" -ForegroundColor White
    } else {
        Write-Host "Please fix the errors before running." -ForegroundColor Red
    }
}

Write-Host "???????????????????????????????????????????????????" -ForegroundColor Gray
