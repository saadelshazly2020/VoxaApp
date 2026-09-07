# Build Production Version
# This script builds both the Vue app and the .NET application

Write-Host "???  Building Video Chat Application for Production" -ForegroundColor Green
Write-Host ""

# Build Vue app
Write-Host "?? Building Vue Client Application..." -ForegroundColor Cyan
cd client-app
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Vue build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? Vue app built successfully!" -ForegroundColor Green
Write-Host "   Output: wwwroot/dist/" -ForegroundColor Gray
cd ..

Write-Host ""
Write-Host "?? Building .NET Application..." -ForegroundColor Cyan
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "? .NET build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? .NET app built successfully!" -ForegroundColor Green

Write-Host ""
Write-Host "?? Production build completed!" -ForegroundColor Green
Write-Host ""
Write-Host "?? To run the production build:" -ForegroundColor Yellow
Write-Host "   dotnet run --configuration Release" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? To publish:" -ForegroundColor Yellow
Write-Host "   dotnet publish -c Release -o ./publish" -ForegroundColor Cyan
