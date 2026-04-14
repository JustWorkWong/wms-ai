using WmsAi.AiGateway.Application.AgUi;

namespace WmsAi.AiGateway.Application.Services;

public interface IAgUiEventTransformer
{
    AgUiEvent? TransformWorkflowEvent(object workflowEvent);
}
