var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.MapGet("/", () => "Conversations API"); app.Run();
