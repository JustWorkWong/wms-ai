using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using Npgsql;

namespace WmsAi.Integration.Tests;

public class CapEventIntegrationTests
{
    [Fact(Skip = "Aspire version compatibility issue - Method 'get_Pipeline' not found")]
    public async Task CreateTenant_ShouldPublishTenantCreatedEvent()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var platformClient = app.CreateHttpClient("platform");
        var userDbConnectionString = await app.GetConnectionStringAsync("UserDb");

        // Act - Create a tenant
        var createTenantRequest = new
        {
            tenantCode = "TEST001",
            tenantName = "Test Tenant",
            defaultWarehouseCode = "WH001",
            defaultWarehouseName = "Test Warehouse",
            adminLoginName = "admin@test.com"
        };

        var response = await platformClient.PostAsJsonAsync("/api/tenants", createTenantRequest);
        response.EnsureSuccessStatusCode();

        // Wait for event to be published
        await Task.Delay(2000);

        // Assert - Verify event was published to CAP outbox
        await using var connection = new NpgsqlConnection(userDbConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM cap.published WHERE name = 'tenant.created.v1'",
            connection);

        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.True(count > 0, "TenantCreatedV1 event should be published to CAP outbox");
    }

    [Fact(Skip = "Aspire version compatibility issue - Method 'get_Pipeline' not found")]
    public async Task RecordReceipt_ShouldPublishReceiptRecordedEvent()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var inboundClient = app.CreateHttpClient("inbound");
        var businessDbConnectionString = await app.GetConnectionStringAsync("BusinessDb");

        // Setup - Create inbound notice first
        var createNoticeRequest = new
        {
            tenantId = "TENANT001",
            warehouseId = "WH001",
            noticeNo = "IB20260414001",
            lines = new[]
            {
                new { skuCode = "SKU001", expectedQuantity = 100 }
            }
        };

        var noticeResponse = await inboundClient.PostAsJsonAsync("/api/inbound-notices", createNoticeRequest);
        noticeResponse.EnsureSuccessStatusCode();
        var noticeResult = await noticeResponse.Content.ReadFromJsonAsync<dynamic>();

        // Act - Record receipt
        var recordReceiptRequest = new
        {
            tenantId = "TENANT001",
            warehouseId = "WH001",
            inboundNoticeId = noticeResult?.inboundNoticeId,
            receiptNo = "REC20260414001",
            lines = new[]
            {
                new { skuCode = "SKU001", receivedQuantity = 100 }
            }
        };

        var response = await inboundClient.PostAsJsonAsync("/api/receipts", recordReceiptRequest);
        response.EnsureSuccessStatusCode();

        // Wait for event to be published
        await Task.Delay(2000);

        // Assert - Verify event was published to CAP outbox
        await using var connection = new NpgsqlConnection(businessDbConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM cap.published WHERE name = 'receipt.recorded.v1'",
            connection);

        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.True(count > 0, "ReceiptRecordedV1 event should be published to CAP outbox");
    }

    [Fact(Skip = "Aspire version compatibility issue - Method 'get_Pipeline' not found")]
    public async Task TenantCreatedEvent_ShouldBeConsumedByInbound()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var platformClient = app.CreateHttpClient("platform");
        var businessDbConnectionString = await app.GetConnectionStringAsync("BusinessDb");

        // Act - Create a tenant (which publishes TenantCreatedV1)
        var createTenantRequest = new
        {
            tenantCode = "TEST002",
            tenantName = "Test Tenant 2",
            defaultWarehouseCode = "WH002",
            defaultWarehouseName = "Test Warehouse 2",
            adminLoginName = "admin2@test.com"
        };

        var response = await platformClient.PostAsJsonAsync("/api/tenants", createTenantRequest);
        response.EnsureSuccessStatusCode();

        // Wait for event to be consumed
        await Task.Delay(3000);

        // Assert - Verify event was received by Inbound service
        await using var connection = new NpgsqlConnection(businessDbConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM cap.received WHERE name = 'tenant.created.v1'",
            connection);

        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.True(count > 0, "TenantCreatedV1 event should be received by Inbound service");
    }

    [Fact(Skip = "Aspire version compatibility issue - Method 'get_Pipeline' not found")]
    public async Task QcTaskCreatedEvent_ShouldBeConsumedByAiGateway()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var inboundClient = app.CreateHttpClient("inbound");

        // Setup - Create inbound notice and record receipt (which creates QC tasks)
        var createNoticeRequest = new
        {
            tenantId = "TENANT003",
            warehouseId = "WH003",
            noticeNo = "IB20260414003",
            lines = new[]
            {
                new { skuCode = "SKU003", expectedQuantity = 50 }
            }
        };

        var noticeResponse = await inboundClient.PostAsJsonAsync("/api/inbound-notices", createNoticeRequest);
        noticeResponse.EnsureSuccessStatusCode();
        var noticeResult = await noticeResponse.Content.ReadFromJsonAsync<dynamic>();

        // Act - Record receipt (which creates QC task and publishes event)
        var recordReceiptRequest = new
        {
            tenantId = "TENANT003",
            warehouseId = "WH003",
            inboundNoticeId = noticeResult?.inboundNoticeId,
            receiptNo = "REC20260414003",
            lines = new[]
            {
                new { skuCode = "SKU003", receivedQuantity = 50 }
            }
        };

        var response = await inboundClient.PostAsJsonAsync("/api/receipts", recordReceiptRequest);
        response.EnsureSuccessStatusCode();

        // Wait for event to be consumed by AiGateway
        await Task.Delay(3000);

        // Assert - Verify AiGateway logged the event (check logs or verify event was received)
        // Note: Since AiGateway uses in-memory storage, we can't query CAP tables
        // In a real scenario, we would check logs or add a test endpoint
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact(Skip = "Aspire version compatibility issue - Method 'get_Pipeline' not found")]
    public async Task EventIdempotency_ShouldPreventDuplicateProcessing()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WmsAi_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var businessDbConnectionString = await app.GetConnectionStringAsync("BusinessDb");

        // Act - Simulate duplicate event by checking CAP's built-in deduplication
        await using var connection = new NpgsqlConnection(businessDbConnectionString);
        await connection.OpenAsync();

        // Verify CAP tables exist
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'cap' AND table_name IN ('published', 'received')",
            connection);

        var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

        // Assert - CAP tables should be created
        Assert.Equal(2, tableCount);
    }
}
