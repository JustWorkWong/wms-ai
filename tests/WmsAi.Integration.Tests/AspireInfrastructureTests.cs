using Aspire.Hosting.Testing;
using Npgsql;
using StackExchange.Redis;
using RabbitMQ.Client;
using Minio;

namespace WmsAi.Integration.Tests;

public class AspireInfrastructureTests
{
    [Fact]
    public async Task PostgreSQL_UserDb_ShouldBeAccessible()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var connectionString = await app.GetConnectionStringAsync("UserDb");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task PostgreSQL_BusinessDb_ShouldBeAccessible()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var connectionString = await app.GetConnectionStringAsync("BusinessDb");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task PostgreSQL_AiDb_ShouldBeAccessible()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var connectionString = await app.GetConnectionStringAsync("AiDb");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task Redis_ShouldBeAccessible()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var connectionString = await app.GetConnectionStringAsync("redis");
        var redis = await ConnectionMultiplexer.ConnectAsync(connectionString!);
        var db = redis.GetDatabase();
        await db.PingAsync();

        // Assert
        Assert.True(redis.IsConnected);
    }

    [Fact]
    public async Task RabbitMQ_ShouldBeAccessible()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var connectionString = await app.GetConnectionStringAsync("rabbitmq");
        var factory = new ConnectionFactory { Uri = new Uri(connectionString!) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Assert
        Assert.True(connection.IsOpen);
        Assert.True(channel.IsOpen);
    }

    [Fact]
    public async Task AllServices_ShouldStartSuccessfully()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act & Assert - Verify all services are registered
        var gatewayClient = app.CreateHttpClient("gateway");
        var platformClient = app.CreateHttpClient("platform");
        var inboundClient = app.CreateHttpClient("inbound");
        var aiGatewayClient = app.CreateHttpClient("ai-gateway");
        var operationsClient = app.CreateHttpClient("operations");

        Assert.NotNull(gatewayClient);
        Assert.NotNull(platformClient);
        Assert.NotNull(inboundClient);
        Assert.NotNull(aiGatewayClient);
        Assert.NotNull(operationsClient);
    }

    [Fact]
    public async Task Platform_HealthCheck_ShouldReturnHealthy()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var client = app.CreateHttpClient("platform");
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Inbound_HealthCheck_ShouldReturnHealthy()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var client = app.CreateHttpClient("inbound");
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
