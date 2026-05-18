var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => new {
    status  = "healthy",
    version = "1.0.0",
    env     = app.Environment.EnvironmentName,
    time    = DateTime.UtcNow
});

app.Run();
