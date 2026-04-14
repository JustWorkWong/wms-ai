using WmsAi.AiGateway.Host.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddCap(options =>
{
    options.UseRabbitMQ(rabbitOptions =>
    {
        rabbitOptions.ConnectionFactoryOptions = factory =>
        {
            factory.Uri = new Uri(rabbitMqConnection);
        };
        rabbitOptions.ExchangeName = "wmsai.events";
    });
});

builder.Services.AddScoped<InboundEventConsumer>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok());
app.MapDefaultEndpoints();

app.Run();
