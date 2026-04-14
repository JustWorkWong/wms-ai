using WmsAi.Gateway.Host.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseMiddleware<FakeIdentityMiddleware>();
app.UseMiddleware<RequestExecutionContextMiddleware>();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();
