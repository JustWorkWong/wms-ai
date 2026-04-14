namespace WmsAi.AiGateway.Domain.Workflows;

public enum StepKind
{
    Preparation,
    DataLoad,
    Validation,
    AgentExecution,
    Normalization,
    Gate,
    Persistence,
    Decision,
    Wait,
    Finalization
}
