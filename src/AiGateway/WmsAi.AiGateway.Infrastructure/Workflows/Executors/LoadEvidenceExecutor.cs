using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Agents;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Infrastructure.Functions;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 加载证据资产(图片、视频、文档等)
/// </summary>
public sealed partial class LoadEvidenceExecutor : Executor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoadEvidenceExecutor> _logger;

    public LoadEvidenceExecutor(
        IServiceProvider serviceProvider,
        ILogger<LoadEvidenceExecutor> logger)
        : base("LoadEvidence")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder builder)
    {
        // 配置协议(如果需要)
        return builder;
    }

    [MessageHandler]
    public async Task<QcInspectionState> HandleAsync(
        QcInspectionState state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "开始加载证据资产: QcTaskId={QcTaskId}, TenantId={TenantId}",
            state.QcTaskId, state.TenantId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var businessApiClient = scope.ServiceProvider.GetRequiredService<IBusinessApiClient>();

            var evidence = await businessApiClient.GetAsync<List<EvidenceAsset>>(
                $"/api/qc/tasks/{state.QcTaskId}/evidence",
                state.TenantId,
                state.WarehouseId,
                cancellationToken);

            if (evidence == null || evidence.Count == 0)
            {
                _logger.LogWarning("未找到证据资产: QcTaskId={QcTaskId}", state.QcTaskId);
                return state.With(evidence: [], status: "EvidenceLoaded");
            }

            _logger.LogInformation(
                "成功加载证据资产: QcTaskId={QcTaskId}, Count={Count}",
                state.QcTaskId, evidence.Count);

            return state.With(evidence: evidence, status: "EvidenceLoaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载证据资产失败: QcTaskId={QcTaskId}", state.QcTaskId);
            return state.WithError($"加载证据资产失败: {ex.Message}");
        }
    }
}
