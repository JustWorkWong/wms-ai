using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Infrastructure.Persistence;
using WmsAi.Platform.Domain.Tenants;
using WmsAi.Platform.Domain.Users;
using WmsAi.Platform.Infrastructure.Persistence;

namespace WmsAi.Operations.Host.Services;

public class SeedDataImporter
{
    private readonly UserDbContext _userDb;
    private readonly AiDbContext _aiDb;
    private readonly ILogger<SeedDataImporter> _logger;
    private readonly string _seedDataPath;

    public SeedDataImporter(
        UserDbContext userDb,
        AiDbContext aiDb,
        ILogger<SeedDataImporter> logger,
        IWebHostEnvironment environment)
    {
        _userDb = userDb;
        _aiDb = aiDb;
        _logger = logger;
        _seedDataPath = Path.Combine(environment.ContentRootPath, "SeedData");
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting seed data import from {Path}", _seedDataPath);

        if (await IsAlreadyImportedAsync(cancellationToken))
        {
            _logger.LogInformation("Seed data already imported, skipping");
            return;
        }

        try
        {
            await ImportTenantsAsync(cancellationToken);
            await ImportWarehousesAsync(cancellationToken);
            await ImportUsersAsync(cancellationToken);
            await ImportMembershipsAsync(cancellationToken);
            await ImportModelProvidersAsync(cancellationToken);
            await ImportModelProfilesAsync(cancellationToken);

            await MarkAsImportedAsync(cancellationToken);
            _logger.LogInformation("Seed data import completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import seed data");
            throw;
        }
    }

    private async Task<bool> IsAlreadyImportedAsync(CancellationToken cancellationToken)
    {
        return await _userDb.Tenants.AnyAsync(t => t.Code == "TENANT_DEMO", cancellationToken);
    }

    private async Task MarkAsImportedAsync(CancellationToken cancellationToken)
    {
        await _userDb.SaveChangesAsync(cancellationToken);
        await _aiDb.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportTenantsAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataPath, "demo-tenants.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Seed file not found: {Path}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<TenantSeedData>>(json);

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No tenants to import");
            return;
        }

        foreach (var item in items)
        {
            var exists = await _userDb.Tenants.AnyAsync(t => t.Code == item.Code, cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Tenant {Code} already exists, skipping", item.Code);
                continue;
            }

            var tenant = new Tenant(item.Code, item.Name);
            SetEntityId(tenant, item.Id);

            _userDb.Tenants.Add(tenant);
            _logger.LogInformation("Imported tenant: {Code} - {Name}", item.Code, item.Name);
        }

        await _userDb.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportWarehousesAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataPath, "demo-warehouses.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Seed file not found: {Path}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<WarehouseSeedData>>(json);

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No warehouses to import");
            return;
        }

        foreach (var item in items)
        {
            var tenantId = Guid.Parse(item.TenantId);
            var exists = await _userDb.Warehouses.AnyAsync(w => w.Code == item.Code && w.TenantId == tenantId, cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Warehouse {Code} already exists, skipping", item.Code);
                continue;
            }

            var warehouse = new Warehouse(tenantId, item.Code, item.Name, false);
            SetEntityId(warehouse, item.Id);

            _userDb.Warehouses.Add(warehouse);
            _logger.LogInformation("Imported warehouse: {Code} - {Name}", item.Code, item.Name);
        }

        await _userDb.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportUsersAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataPath, "demo-users.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Seed file not found: {Path}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<UserSeedData>>(json);

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No users to import");
            return;
        }

        foreach (var item in items)
        {
            var exists = await _userDb.Users.AnyAsync(u => u.LoginName == item.LoginName, cancellationToken);
            if (exists)
            {
                _logger.LogInformation("User {LoginName} already exists, skipping", item.LoginName);
                continue;
            }

            var user = new User(item.LoginName);
            SetEntityId(user, item.Id);

            _userDb.Users.Add(user);
            _logger.LogInformation("Imported user: {LoginName} - {DisplayName}", item.LoginName, item.DisplayName);
        }

        await _userDb.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportMembershipsAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataPath, "demo-memberships.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Seed file not found: {Path}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<MembershipSeedData>>(json);

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No memberships to import");
            return;
        }

        foreach (var item in items)
        {
            var tenantId = Guid.Parse(item.TenantId);
            var warehouseId = Guid.Parse(item.WarehouseId);
            var userId = Guid.Parse(item.UserId);

            var exists = await _userDb.Memberships.AnyAsync(
                m => m.TenantId == tenantId && m.WarehouseId == warehouseId && m.UserId == userId,
                cancellationToken);

            if (exists)
            {
                _logger.LogInformation("Membership for user {UserId} already exists, skipping", item.UserId);
                continue;
            }

            var membership = new Membership(tenantId, warehouseId, userId, item.Role);
            SetEntityId(membership, item.Id);

            _userDb.Memberships.Add(membership);
            _logger.LogInformation("Imported membership: {UserId} - {Role}", item.UserId, item.Role);
        }

        await _userDb.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportModelProvidersAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataPath, "demo-model-providers.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Seed file not found: {Path}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<ModelProviderSeedData>>(json);

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No model providers to import");
            return;
        }

        foreach (var item in items)
        {
            var exists = await _aiDb.AiModelProviders.AnyAsync(p => p.ProviderCode == item.Code, cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Model provider {Code} already exists, skipping", item.Code);
                continue;
            }

            var provider = new AiModelProvider(
                item.Code,
                item.Name,
                item.BaseUrl,
                null,
                "api_key",
                item.ApiKeyEnvVar);

            SetEntityId(provider, item.Id);

            if (item.IsEnabled)
            {
                provider.Activate();
            }

            _aiDb.AiModelProviders.Add(provider);
            _logger.LogInformation("Imported model provider: {Code} - {Name}", item.Code, item.Name);
        }

        await _aiDb.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportModelProfilesAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataPath, "demo-model-profiles.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Seed file not found: {Path}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<ModelProfileSeedData>>(json);

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("No model profiles to import");
            return;
        }

        foreach (var item in items)
        {
            var exists = await _aiDb.AiModelProfiles.AnyAsync(p => p.ProfileCode == item.Code, cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Model profile {Code} already exists, skipping", item.Code);
                continue;
            }

            var provider = await _aiDb.AiModelProviders.FirstOrDefaultAsync(p => p.ProviderCode == item.ProviderCode, cancellationToken);
            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderCode} not found for profile {Code}, skipping", item.ProviderCode, item.Code);
                continue;
            }

            var profile = new AiModelProfile(
                provider.Id,
                item.Code,
                item.SceneCode,
                item.ModelName,
                (decimal)item.Temperature,
                null,
                item.MaxTokens,
                item.TimeoutSeconds,
                null,
                null,
                item.IsEnabled);

            SetEntityId(profile, item.Id);

            _aiDb.AiModelProfiles.Add(profile);
            _logger.LogInformation("Imported model profile: {Code} - {ModelName}", item.Code, item.ModelName);
        }

        await _aiDb.SaveChangesAsync(cancellationToken);
    }

    private static void SetEntityId(object entity, string id)
    {
        var guidId = Guid.Parse(id);
        var idProperty = entity.GetType().GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(entity, guidId);
        }
    }

    private record TenantSeedData(string Id, string Code, string Name, string Status);
    private record WarehouseSeedData(string Id, string TenantId, string Code, string Name, string Status);
    private record UserSeedData(string Id, string LoginName, string DisplayName, string Status);
    private record MembershipSeedData(string Id, string TenantId, string WarehouseId, string UserId, string Role, string Status);
    private record ModelProviderSeedData(string Id, string Code, string Name, string BaseUrl, string ApiKeyEnvVar, bool IsEnabled);
    private record ModelProfileSeedData(string Id, string Code, string SceneCode, string ProviderCode, string ModelName, double Temperature, int MaxTokens, int TimeoutSeconds, bool IsEnabled);
}
