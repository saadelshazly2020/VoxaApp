# Deployment Guide

## Local Development

### Prerequisites
- .NET 9 SDK installed
- Port 5000/5001 available

### Steps
1. Navigate to project directory
2. Run `dotnet restore`
3. Run `dotnet run`
4. Open browser to `https://localhost:5001`

## Docker Deployment

### Build Docker Image
```bash
docker build -t videochat-app .
```

### Run Container
```bash
docker run -p 8080:8080 videochat-app
```

## Azure App Service

### Using Azure CLI
```bash
# Login
az login

# Create resource group
az group create --name videochat-rg --location eastus

# Create app service plan
az appservice plan create --name videochat-plan --resource-group videochat-rg --sku B1 --is-linux

# Create web app
az webapp create --resource-group videochat-rg --plan videochat-plan --name videochat-app --runtime "DOTNET|9.0"

# Deploy
az webapp deploy --resource-group videochat-rg --name videochat-app --src-path ./publish.zip
```

### Configure App Service
1. Enable WebSockets in Configuration
2. Add HTTPS binding
3. Configure CORS if needed

## IIS Deployment

### Prerequisites
- Windows Server with IIS installed
- .NET 9 Hosting Bundle
- URL Rewrite module

### Steps
1. Publish application:
```bash
dotnet publish -c Release -o ./publish
```

2. Copy files to IIS directory (e.g., `C:\inetpub\wwwroot\videochat`)

3. Create Application Pool:
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated

4. Create Website:
   - Bind to port 443 (HTTPS required for WebRTC)
   - Point to application directory
   - Select application pool

5. Configure web.config:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\VideoChatingApp.WebRTC.dll" 
                stdoutLogEnabled="false" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
    <webSocket enabled="true" />
  </system.webServer>
</configuration>
```

## Nginx Reverse Proxy

### Configuration
```nginx
server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## TURN Server Setup (Coturn)

### Installation (Ubuntu/Debian)
```bash
sudo apt-get update
sudo apt-get install coturn
```

### Configuration (/etc/turnserver.conf)
```bash
listening-port=3478
listening-ip=0.0.0.0
relay-ip=YOUR_SERVER_IP
external-ip=YOUR_PUBLIC_IP

realm=yourdomain.com
server-name=yourdomain.com

lt-cred-mech
user=webrtcuser:StrongPassword123

fingerprint
no-multicast-peers

# Security
secure-stun
no-tlsv1
no-tlsv1_1
```

### Enable and Start
```bash
sudo systemctl enable coturn
sudo systemctl start coturn
```

### Test TURN Server
Use online tools like:
- https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/
- Input your TURN server credentials

## SSL/TLS Certificate

### Let's Encrypt (Certbot)
```bash
# Install certbot
sudo apt-get install certbot

# Generate certificate
sudo certbot certonly --standalone -d yourdomain.com

# Auto-renewal
sudo certbot renew --dry-run
```

## Environment Variables

Create `appsettings.Production.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/path/to/cert.pfx",
          "Password": "cert-password"
        }
      }
    }
  }
}
```

## Performance Tuning

### SignalR Configuration
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false; // Disable in production
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 102400; // 100KB
});
```

### Kestrel Configuration
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.MaxRequestBodySize = 10485760; // 10MB
});
```

## Monitoring

### Application Insights (Azure)
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Health Checks
```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

### Logging
Configure structured logging with Serilog:
```bash
dotnet add package Serilog.AspNetCore
```

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
});
```

## Backup & Recovery

### Database (if added)
```bash
# Backup
pg_dump -U username dbname > backup.sql

# Restore
psql -U username dbname < backup.sql
```

### Configuration Files
- Backup appsettings.json
- Backup SSL certificates
- Document TURN server configuration

## Troubleshooting

### Check Application Logs
```bash
# Linux
tail -f /var/log/videochat/app.log

# Windows
type C:\inetpub\logs\videochat\app.log
```

### Test SignalR Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://yourdomain.com/videocallhub")
    .build();

connection.start()
    .then(() => console.log("Connected"))
    .catch(err => console.error(err));
```

### Verify TURN Server
```bash
# Check if running
sudo systemctl status coturn

# Test connectivity
telnet YOUR_SERVER_IP 3478
```

### Common Issues

**Issue**: SignalR disconnects frequently
**Solution**: Increase timeout in client and server configuration

**Issue**: Video doesn't connect
**Solution**: Verify TURN server is accessible and credentials are correct

**Issue**: High CPU usage
**Solution**: Limit concurrent connections, use SFU instead of mesh for large rooms

## Security Checklist

- [ ] HTTPS enabled
- [ ] CORS configured properly
- [ ] Authentication implemented
- [ ] TURN server credentials secured
- [ ] Rate limiting enabled
- [ ] Input validation on all endpoints
- [ ] Detailed errors disabled in production
- [ ] Firewall rules configured
- [ ] Regular security updates
- [ ] SSL certificate auto-renewal configured
