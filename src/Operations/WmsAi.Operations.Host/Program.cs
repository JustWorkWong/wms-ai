var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/", () => Results.Ok());
app.MapDefaultEndpoints();

app.Run();
