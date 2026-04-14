using WmsAi.AiGateway.Application.AgUi;
using WmsAi.AiGateway.Application.Services;

namespace WmsAi.AiGateway.Infrastructure.Services;

public sealed class AgUiEventTransformer : IAgUiEventTransformer
{
    public AgUiEvent? TransformWorkflowEvent(object workflowEvent)
    {
        return workflowEvent switch
        {
            WorkflowStartedEvent e => new AgUiStatusEvent("running", CurrentNode: e.WorkflowName),
            WorkflowStepStartedEvent e => new AgUiStatusEvent("running", CurrentNode: e.NodeName),
            AgentMessageGeneratedEvent e => new AgUiMessageEvent(e.Content, Role: "assistant"),
            ToolCallStartedEvent e => new AgUiToolCallEvent(e.ToolName, "running"),
            ToolCallCompletedEvent e => new AgUiToolCallEvent(e.ToolName, "completed", Result: e.Result),
            WorkflowCompletedEvent e => new AgUiStatusEvent("completed", Confidence: e.Confidence),
            WorkflowFailedEvent e => new AgUiErrorEvent(e.ErrorMessage, ErrorCode: "WORKFLOW_FAILED"),
            _ => null
        };
    }
}

// Workflow event types (these would be defined in the workflow engine)
public record WorkflowStartedEvent(string WorkflowName);
public record WorkflowStepStartedEvent(string NodeName);
public record AgentMessageGeneratedEvent(string Content);
public record ToolCallStartedEvent(string ToolName);
public record ToolCallCompletedEvent(string ToolName, string Result);
public record WorkflowCompletedEvent(double? Confidence);
public record WorkflowFailedEvent(string ErrorMessage);
public record SuperStepCompletedEvent(string StepName);
