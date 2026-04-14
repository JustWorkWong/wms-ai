using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Infrastructure.Functions;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 加载质量规则(质检标准、合格判定规则等)
/// </summary>
public sealed partial class LoadRulesExecutor : Executor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoadRulesExecutor> _logger;

    public LoadRulesExecutor(
        IServiceProvider serviceProvider,
        ILogger<LoadRulesExecutor> logger)
        : base("LoadRules")
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
            "开始加载质量规则: QcTaskId={QcTaskId}, SkuCode={SkuCode}",
            state.QcTaskId, state.QcTask?.SkuCode);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var businessApiClient = scope.ServiceProvider.GetRequiredService<IBusinessApiClient>();

            // 根据 SKU 加载质量规则
            var skuCode = state.QcTask?.SkuCode ?? string.Empty;
            if (string.IsNullOrEmpty(skuCode))
            {
                _logger.LogWarning("SKU 编码为空,无法加载质量规则: QcTaskId={QcTaskId}", state.QcTaskId);
                return new QcInspectionState
                {
                    QcTaskId = state.QcTaskId,
                    TenantId = state.TenantId,
                    WarehouseId = state.WarehouseId,
                    UserId = state.UserId,
                    WorkflowRunId = state.WorkflowRunId,
                    QcTask = state.QcTask,
                    Evidence = state.Evidence,
                    QualityRules = new Dictionary<string, object>(),
                    Status = "RulesLoaded"
                };
            }

            var rules = await businessApiClient.GetAsync<Dictionary<string, object>>(
                $"/api/qc/rules?skuCode={skuCode}",
                state.TenantId,
                state.WarehouseId,
                cancellationToken);

            if (rules == null || rules.Count == 0)
            {
                _logger.LogWarning(
                    "未找到质量规则: QcTaskId={QcTaskId}, SkuCode={SkuCode}",
                    state.QcTaskId, skuCode);
                return new QcInspectionState
                {
                    QcTaskId = state.QcTaskId,
                    TenantId = state.TenantId,
                    WarehouseId = state.WarehouseId,
                    UserId = state.UserId,
                    WorkflowRunId = state.WorkflowRunId,
                    QcTask = state.QcTask,
                    Evidence = state.Evidence,
                    QualityRules = new Dictionary<string, object>(),
                    Status = "RulesLoaded"
                };
            }

            _logger.LogInformation(
                "成功加载质量规则: QcTaskId={QcTaskId}, SkuCode={SkuCode}, RuleCount={Count}",
                state.QcTaskId, skuCode, rules.Count);

            return new QcInspectionState
            {
                QcTaskId = state.QcTaskId,
                TenantId = state.TenantId,
                WarehouseId = state.WarehouseId,
                UserId = state.UserId,
                WorkflowRunId = state.WorkflowRunId,
                QcTask = state.QcTask,
                Evidence = state.Evidence,
                QualityRules = rules,
                Status = "RulesLoaded"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载质量规则失败: QcTaskId={QcTaskId}", state.QcTaskId);
            return new QcInspectionState
            {
                QcTaskId = state.QcTaskId,
                TenantId = state.TenantId,
                WarehouseId = state.WarehouseId,
                UserId = state.UserId,
                WorkflowRunId = state.WorkflowRunId,
                QcTask = state.QcTask,
                Evidence = state.Evidence,
                Status = "Failed",
                ErrorMessage = $"加载质量规则失败: {ex.Message}"
            };
        }
    }
}
