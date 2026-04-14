using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WmsAi.AiGateway.Application.Workflows;
using WmsAi.AiGateway.Infrastructure.Functions;

namespace WmsAi.AiGateway.Infrastructure.Workflows.Executors;

/// <summary>
/// 从 Inbound 服务加载质检任务详情
/// </summary>
public sealed partial class LoadQcTaskExecutor : Executor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoadQcTaskExecutor> _logger;

    public LoadQcTaskExecutor(
        IServiceProvider serviceProvider,
        ILogger<LoadQcTaskExecutor> logger)
        : base("LoadQcTask")
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
            "开始加载质检任务详情: QcTaskId={QcTaskId}, TenantId={TenantId}, WarehouseId={WarehouseId}",
            state.QcTaskId, state.TenantId, state.WarehouseId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var businessApiClient = scope.ServiceProvider.GetRequiredService<IBusinessApiClient>();

            var qcTask = await businessApiClient.GetAsync<QcTaskDetail>(
                $"/api/qc/tasks/{state.QcTaskId}",
                state.TenantId,
                state.WarehouseId,
                cancellationToken);

            if (qcTask == null)
            {
                _logger.LogWarning("质检任务不存在: QcTaskId={QcTaskId}", state.QcTaskId);
                return state.WithError($"质检任务不存在: {state.QcTaskId}");
            }

            _logger.LogInformation(
                "成功加载质检任务: QcTaskId={QcTaskId}, SkuCode={SkuCode}, Quantity={Quantity}",
                qcTask.QcTaskId, qcTask.SkuCode, qcTask.Quantity);

            return state.With(qcTask: qcTask, status: "QcTaskLoaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载质检任务失败: QcTaskId={QcTaskId}", state.QcTaskId);
            return state.WithError($"加载质检任务失败: {ex.Message}");
        }
    }
}
