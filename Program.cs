using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VideoChatingApp.WebRTC.Core.Interfaces;
using VideoChatingApp.WebRTC.Core.Services;
using VideoChatingApp.WebRTC.Data;
using VideoChatingApp.WebRTC.Hubs;
using VideoChatingApp.WebRTC.Managers;

var builder = WebApplication.CreateBuilder(args);

// Add Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddControllers();

// Add Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VideoChatingApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VideoChatingAppUsers";

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be set via Jwt:Key in config or Jwt__Key environment variable. " +
        "It must be at least 32 characters. Generate one with: openssl rand -base64 32");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        
        // Support SignalR JWT from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<ITutoringService, TutoringService>();
builder.Services.AddHostedService<SessionReminderService>();
builder.Services.AddSingleton<IUserManager, UserManager>();
builder.Services.AddSingleton<IRoomManager, RoomManager>();

// Add SignalR user-connection mapping for chat (a user can have several connections/tabs)
builder.Services.AddSingleton<IDictionary<int, ICollection<string>>>(
    new ConcurrentDictionary<int, ICollection<string>>());

// In-call ("busy") state per user id, shared by the video chat page and the chat dashboard
builder.Services.AddSingleton<IDictionary<string, bool>>(
    new ConcurrentDictionary<string, bool>());

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000", 
            "https://localhost:3000",
            "https://4e97-194-238-97-224.ngrok-free.app",
            "http://localhost:5274",
            "https://localhost:5274"
        )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add SPA static files
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "client-app/dist";
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Serve static files from wwwroot
app.UseStaticFiles();
app.UseSpaStaticFiles();// Map SignalR hub
app.MapHub<VideoCallHub>("/videocallhub");

// Map API controllers
app.MapControllers();

// Configure SPA
app.UseSpa(spa =>
{
    spa.Options.SourcePath = "client-app/dist";

    //if (app.Environment.IsDevelopment())
    //{
    //    // In development, proxy requests to the Vite dev server
    //    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
    //}
});

// Fallback to index.html for SPA routing (when proxy is not used)
//app.MapFallbackToFile("index.html");
// Run once to generate keys:
var keys = WebPush.VapidHelper.GenerateVapidKeys();
Console.WriteLine(keys.PublicKey);
Console.WriteLine(keys.PrivateKey);
app.Run();
