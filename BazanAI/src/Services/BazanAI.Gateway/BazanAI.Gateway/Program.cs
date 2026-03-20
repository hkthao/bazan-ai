using BazanAI.Gateway.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// YARP
app.MapReverseProxy();

app.Run();
