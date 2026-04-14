using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.AiGateway.Infrastructure.Persistence;
using WmsAi.AiGateway.Infrastructure.Repositories;
using WmsAi.SharedKernel.Persistence;
using Xunit;

namespace WmsAi.ArchitectureTests;

public class AiWorkflowPersistenceTests
{
    [Fact]
    public async Task Workflow_repository_should_insert_new_step_runs_when_updating_existing_workflow()
    {
        using var keeper = new SqliteConnection("Data Source=file:ai-workflow-tests?mode=memory&cache=shared");
        await keeper.OpenAsync();

        var options = new DbContextOptionsBuilder<AiDbContext>()
            .UseSqlite("Data Source=file:ai-workflow-tests?mode=memory&cache=shared")
            .AddInterceptors(new VersionedEntitySaveChangesInterceptor())
            .Options;

        await using (var setup = new AiDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid workflowId;

        await using (var dbContext = new AiDbContext(options))
        {
            var repository = new MafWorkflowRunRepository(dbContext);
            var workflowRun = new MafWorkflowRun(
                tenantId: "tenant-a",
                warehouseId: "wh-1",
                workflowName: "QC_INSPECTION",
                agentProfileCode: "QC_DUAL_AGENT",
                requestedBy: "system",
                membershipId: null,
                userInput: "{}",
                routingJson: null,
                executionContextJson: null);

            await repository.AddAsync(workflowRun);

            workflowRun.Start();
            await repository.UpdateAsync(workflowRun);
            workflowRun.Version.Should().Be(2);

            workflowRun.AddStepRun(
                nodeName: "EvidenceGapAnalysis",
                agentProfileCode: "EVIDENCE_GAP_AGENT",
                stepKind: StepKind.AgentExecution,
                inputJson: "{}",
                payloadJson: "{}",
                evidenceJson: null);

            await repository.UpdateAsync(workflowRun);

            // 模拟真实工作流：第一次保存后上下文已重置，随后再追加第二个步骤。
            workflowRun.AddStepRun(
                nodeName: "InspectionDecision",
                agentProfileCode: "INSPECTION_DECISION_AGENT",
                stepKind: StepKind.AgentExecution,
                inputJson: "{}",
                payloadJson: "{}",
                evidenceJson: null);

            var act = async () => await repository.UpdateAsync(workflowRun);

            await act.Should().NotThrowAsync();

            workflowId = workflowRun.Id;
        }

        await using var verificationContext = new AiDbContext(options);
        var savedWorkflow = await verificationContext.MafWorkflowRuns
            .Include(w => w.StepRuns)
            .SingleAsync(w => w.Id == workflowId);

        savedWorkflow.StepRuns.Should().HaveCount(2);
        savedWorkflow.StepRuns.Select(step => step.NodeName)
            .Should()
            .ContainInOrder("EvidenceGapAnalysis", "InspectionDecision");
        savedWorkflow.StepRuns.Select(step => step.Sequence)
            .Should()
            .ContainInOrder(1, 2);
    }
}
