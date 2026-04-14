using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WmsAi.AiGateway.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL 实现的 Checkpoint 存储
/// 实现 MAF ICheckpointStore 接口，将 Workflow Checkpoint 持久化到数据库
/// </summary>
public sealed class PostgresCheckpointStore : ICheckpointStore<JsonElement>
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<PostgresCheckpointStore> _logger;

    public PostgresCheckpointStore(
        AiDbContext dbContext,
        ILogger<PostgresCheckpointStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 创建新的 Checkpoint
    /// </summary>
    public async ValueTask<CheckpointInfo> CreateCheckpointAsync(
        string sessionId,
        JsonElement value,
        CheckpointInfo? parent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        try
        {
            // 生成 CheckpointId
            var checkpointId = Guid.NewGuid().ToString();
            var checkpointInfo = new CheckpointInfo(sessionId, checkpointId);

            // 序列化 Checkpoint 数据
            var checkpointJson = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // 查找关联的 MafSession
            var session = await _dbContext.MafSessions
                .FirstOrDefaultAsync(s => s.Id.ToString() == sessionId);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found, creating checkpoint without session reference", sessionId);
            }

            // 创建 WorkflowCheckpoint 实体
            var entity = new WorkflowCheckpoint
            {
                Id = Guid.Parse(checkpointId),
                SessionId = sessionId,
                ParentCheckpointId = parent?.CheckpointId,
                CheckpointData = checkpointJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.WorkflowCheckpoints.Add(entity);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Created checkpoint {CheckpointId} for session {SessionId}, parent: {ParentId}",
                checkpointId, sessionId, parent?.CheckpointId ?? "none");

            return checkpointInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkpoint for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// 检索指定的 Checkpoint
    /// </summary>
    public async ValueTask<JsonElement> RetrieveCheckpointAsync(
        string sessionId,
        CheckpointInfo key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var entity = await _dbContext.WorkflowCheckpoints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.Id.ToString() == key.CheckpointId);

            if (entity == null)
            {
                _logger.LogWarning(
                    "Checkpoint {CheckpointId} not found for session {SessionId}",
                    key.CheckpointId, sessionId);
                return default;
            }

            // 反序列化 JSON
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(entity.CheckpointData);

            _logger.LogInformation(
                "Retrieved checkpoint {CheckpointId} for session {SessionId}",
                key.CheckpointId, sessionId);

            return jsonElement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve checkpoint {CheckpointId} for session {SessionId}",
                key.CheckpointId, sessionId);
            throw;
        }
    }

    /// <summary>
    /// 检索 Checkpoint 索引（用于恢复时查找可用的 Checkpoint）
    /// </summary>
    public async ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(
        string sessionId,
        CheckpointInfo? withParent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        try
        {
            var query = _dbContext.WorkflowCheckpoints
                .AsNoTracking()
                .Where(c => c.SessionId == sessionId);

            // 如果指定了 parent，只返回该 parent 的子 Checkpoint
            if (withParent != null)
            {
                query = query.Where(c => c.ParentCheckpointId == withParent.CheckpointId);
            }

            var entities = await query
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var checkpoints = entities
                .Select(e => new CheckpointInfo(sessionId, e.Id.ToString()))
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} checkpoints for session {SessionId}, parent: {ParentId}",
                checkpoints.Count, sessionId, withParent?.CheckpointId ?? "none");

            return checkpoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve checkpoint index for session {SessionId}",
                sessionId);
            throw;
        }
    }
}

/// <summary>
/// Workflow Checkpoint 实体（用于持久化 MAF Checkpoint）
/// </summary>
public sealed class WorkflowCheckpoint
{
    public Guid Id { get; set; }

    /// <summary>
    /// Session ID（对应 MafSession.Id）
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 父 Checkpoint ID（用于构建 Checkpoint 树）
    /// </summary>
    public string? ParentCheckpointId { get; set; }

    /// <summary>
    /// Checkpoint 数据（JSON 格式）
    /// </summary>
    public string CheckpointData { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
