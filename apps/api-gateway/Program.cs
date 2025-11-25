using MarketPulseRT.Hubs;
using MarketPulseRT.Services;

var builder = WebApplication.CreateBuilder(args);

// Allow HTTP/2 unencrypted for gRPC client to http://localhost:5001
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Background relay that connects to MarketDataService via gRPC
builder.Services.AddHostedService<MarketStreamerRelay>();

// CORS so Vite dev server can talk to us
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000", // legacy dev origin
                "http://localhost:5173", // Vite dev
                "http://localhost:4173") // Vite preview
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors();

app.MapControllers();
app.MapHub<MarketHub>("/hubs/market");

app.Run();

public partial class Program { }
