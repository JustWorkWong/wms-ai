using Microsoft.EntityFrameworkCore;
using WmsAi.AiGateway.Domain.Inspections;
using WmsAi.AiGateway.Domain.MafSessions;
using WmsAi.AiGateway.Domain.ModelConfig;
using WmsAi.AiGateway.Domain.Workflows;
using WmsAi.SharedKernel.Persistence;

namespace WmsAi.AiGateway.Infrastructure.Persistence;

public sealed class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    // MAF Runtime Tables
    public DbSet<MafSession> MafSessions => Set<MafSession>();
    public DbSet<MafMessage> MafMessages => Set<MafMessage>();
    public DbSet<MafCheckpoint> MafCheckpoints => Set<MafCheckpoint>();
    public DbSet<MafSummarySnapshot> MafSummarySnapshots => Set<MafSummarySnapshot>();
    public DbSet<MafWorkflowRun> MafWorkflowRuns => Set<MafWorkflowRun>();
    public DbSet<MafWorkflowStepRun> MafWorkflowStepRuns => Set<MafWorkflowStepRun>();
    public DbSet<MafToolCallLog> MafToolCallLogs => Set<MafToolCallLog>();
    public DbSet<MafModelCallLog> MafModelCallLogs => Set<MafModelCallLog>();

    // AI Business Tables
    public DbSet<AiInspectionRun> AiInspectionRuns => Set<AiInspectionRun>();
    public DbSet<AiSuggestion> AiSuggestions => Set<AiSuggestion>();
    public DbSet<AiModelProvider> AiModelProviders => Set<AiModelProvider>();
    public DbSet<AiModelProfile> AiModelProfiles => Set<AiModelProfile>();
    public DbSet<AiRoutingPolicy> AiRoutingPolicies => Set<AiRoutingPolicy>();

    // Workflow Checkpoint Storage
    public DbSet<WorkflowCheckpoint> WorkflowCheckpoints => Set<WorkflowCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureMafSessions(modelBuilder);
        ConfigureMafWorkflows(modelBuilder);
        ConfigureInspections(modelBuilder);
        ConfigureModelConfig(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureWorkflowCheckpoints(modelBuilder);
    }

    private static void ConfigureMafSessions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MafSession>(builder =>
        {
            builder.ToTable("maf_sessions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.SessionType).HasMaxLength(64).IsRequired();
            builder.Property(e => e.BusinessObjectType).HasMaxLength(128).IsRequired();
            builder.Property(e => e.BusinessObjectId).HasMaxLength(128).IsRequired();
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(e => e.AgentSessionJson).HasColumnType("jsonb");
            builder.Property(e => e.LastCheckpointId);
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();
            builder.HasIndex(e => new { e.TenantId, e.BusinessObjectType, e.BusinessObjectId });
            builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.Status });
            builder.HasIndex(e => e.UserId);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<MafMessage>(builder =>
        {
            builder.ToTable("maf_messages");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.SessionId).IsRequired();
            builder.Property(e => e.Sequence).IsRequired();
            builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(32);
            builder.Property(e => e.MessageType).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ContentText).HasColumnType("text");
            builder.Property(e => e.ContentJson).HasColumnType("jsonb");
            builder.Property(e => e.IsSummary).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => new { e.SessionId, e.Sequence }).IsUnique();
            builder.HasOne<MafSession>()
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MafCheckpoint>(builder =>
        {
            builder.ToTable("maf_checkpoints");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.SessionId).IsRequired();
            builder.Property(e => e.CheckpointName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.WorkflowRunId);
            builder.Property(e => e.WorkflowStepRunId);
            builder.Property(e => e.SummarySnapshotId);
            builder.Property(e => e.Cursor).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => new { e.SessionId, e.CheckpointName });
            builder.HasOne<MafSession>()
                .WithMany(s => s.Checkpoints)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MafSummarySnapshot>(builder =>
        {
            builder.ToTable("maf_summary_snapshots");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.SessionId).IsRequired();
            builder.Property(e => e.SummaryText).HasColumnType("text").IsRequired();
            builder.Property(e => e.EvidenceRefsJson).HasColumnType("jsonb");
            builder.Property(e => e.MessageRangeJson).HasColumnType("jsonb");
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => e.SessionId);
            builder.HasOne<MafSession>()
                .WithMany(s => s.SummarySnapshots)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMafWorkflows(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MafWorkflowRun>(builder =>
        {
            builder.ToTable("maf_workflow_runs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WorkflowName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.AgentProfileCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(e => e.RequestedBy).HasMaxLength(64).IsRequired();
            builder.Property(e => e.MembershipId).HasMaxLength(64);
            builder.Property(e => e.UserInput).HasColumnType("text");
            builder.Property(e => e.RoutingJson).HasColumnType("jsonb");
            builder.Property(e => e.ExecutionContextJson).HasColumnType("jsonb");
            builder.Property(e => e.ResultJson).HasColumnType("jsonb");
            builder.Property(e => e.ErrorMessage).HasColumnType("text");
            builder.Property(e => e.CurrentNode).HasMaxLength(128);
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();
            builder.Property(e => e.CompletedAt);
            builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.Status });
            builder.HasIndex(e => new { e.WorkflowName, e.Status });
            builder.HasIndex(e => e.CreatedAt);
            builder.Navigation(e => e.StepRuns)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Metadata.FindNavigation(nameof(MafWorkflowRun.StepRuns))!
                .SetField("_stepRuns");
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<MafWorkflowStepRun>(builder =>
        {
            builder.ToTable("maf_workflow_step_runs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.WorkflowRunId).IsRequired();
            builder.Property(e => e.Sequence).IsRequired();
            builder.Property(e => e.NodeName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.AgentProfileCode).HasMaxLength(64);
            builder.Property(e => e.StepKind).HasConversion<string>().HasMaxLength(32);
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(e => e.AttemptCount).IsRequired();
            builder.Property(e => e.Message).HasColumnType("text");
            builder.Property(e => e.InputJson).HasColumnType("jsonb");
            builder.Property(e => e.PayloadJson).HasColumnType("jsonb");
            builder.Property(e => e.EvidenceJson).HasColumnType("jsonb");
            builder.Property(e => e.ErrorMessage).HasColumnType("text");
            builder.Property(e => e.StartedAt);
            builder.Property(e => e.CompletedAt);
            builder.HasIndex(e => new { e.WorkflowRunId, e.Sequence }).IsUnique();
            builder.HasOne<MafWorkflowRun>()
                .WithMany(w => w.StepRuns)
                .HasForeignKey(e => e.WorkflowRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInspections(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiInspectionRun>(builder =>
        {
            builder.ToTable("ai_inspection_runs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.QcTaskId).IsRequired();
            builder.Property(e => e.WorkflowRunId).IsRequired();
            builder.Property(e => e.SessionId);
            builder.Property(e => e.AgentProfileCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ModelProfileCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ModelConfigSnapshotJson).HasColumnType("jsonb");
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(e => e.ResultSummary).HasColumnType("text");
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();
            builder.Property(e => e.CompletedAt);
            builder.HasIndex(e => e.QcTaskId).IsUnique();
            builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.Status });
            builder.HasIndex(e => e.WorkflowRunId);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<AiSuggestion>(builder =>
        {
            builder.ToTable("ai_suggestions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.QcTaskId).IsRequired();
            builder.Property(e => e.SuggestionType).HasMaxLength(64).IsRequired();
            builder.Property(e => e.Reasoning).HasColumnType("text").IsRequired();
            builder.Property(e => e.Confidence).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => new { e.TenantId, e.QcTaskId });
        });
    }

    private static void ConfigureModelConfig(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiModelProvider>(builder =>
        {
            builder.ToTable("ai_model_providers");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.ProviderCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ProviderName).HasMaxLength(256).IsRequired();
            builder.Property(e => e.ApiBaseUrl).HasMaxLength(512).IsRequired();
            builder.Property(e => e.ApiVersion).HasMaxLength(32);
            builder.Property(e => e.AuthMode).HasMaxLength(32).IsRequired();
            builder.Property(e => e.CredentialRef).HasMaxLength(256).IsRequired();
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            builder.HasIndex(e => e.ProviderCode).IsUnique();
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<AiModelProfile>(builder =>
        {
            builder.ToTable("ai_model_profiles");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.ProviderId).IsRequired();
            builder.Property(e => e.ProfileCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.SceneCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ModelName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.Temperature).HasPrecision(4, 2).IsRequired();
            builder.Property(e => e.TopP).HasPrecision(4, 2);
            builder.Property(e => e.MaxTokens).IsRequired();
            builder.Property(e => e.TimeoutSeconds).IsRequired();
            builder.Property(e => e.RetryPolicyJson).HasColumnType("jsonb");
            builder.Property(e => e.PromptAssetVersion).HasMaxLength(64);
            builder.Property(e => e.IsActive).IsRequired();
            builder.HasIndex(e => e.ProfileCode).IsUnique();
            builder.HasIndex(e => new { e.SceneCode, e.IsActive });
            builder.HasOne<AiModelProvider>()
                .WithMany()
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });

        modelBuilder.Entity<AiRoutingPolicy>(builder =>
        {
            builder.ToTable("ai_routing_policies");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.PolicyName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.SceneCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64);
            builder.Property(e => e.RoutingRulesJson).HasColumnType("jsonb").IsRequired();
            builder.Property(e => e.Priority).IsRequired();
            builder.Property(e => e.IsActive).IsRequired();
            builder.HasIndex(e => new { e.TenantId, e.SceneCode, e.Priority });
            VersionedEntityTypeConfiguration.ApplyVersion(builder);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MafToolCallLog>(builder =>
        {
            builder.ToTable("maf_tool_call_logs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.SessionId);
            builder.Property(e => e.WorkflowRunId);
            builder.Property(e => e.WorkflowStepRunId);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.MembershipId).HasMaxLength(64);
            builder.Property(e => e.CallType).HasMaxLength(32).IsRequired();
            builder.Property(e => e.ToolName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.InputJson).HasColumnType("jsonb");
            builder.Property(e => e.OutputJson).HasColumnType("jsonb");
            builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
            builder.Property(e => e.DurationMs).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.CreatedAt });
            builder.HasIndex(e => e.WorkflowRunId);
            builder.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<MafModelCallLog>(builder =>
        {
            builder.ToTable("maf_model_call_logs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.SessionId);
            builder.Property(e => e.WorkflowRunId);
            builder.Property(e => e.WorkflowStepRunId);
            builder.Property(e => e.AgentProfileCode).HasMaxLength(64);
            builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.WarehouseId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.UserId).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ProviderCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ModelName).HasMaxLength(128).IsRequired();
            builder.Property(e => e.ProfileCode).HasMaxLength(64).IsRequired();
            builder.Property(e => e.RequestTokens).IsRequired();
            builder.Property(e => e.ResponseTokens).IsRequired();
            builder.Property(e => e.TotalTokens).IsRequired();
            builder.Property(e => e.LatencyMs).IsRequired();
            builder.Property(e => e.FinishReason).HasMaxLength(64).IsRequired();
            builder.Property(e => e.RequestMetaJson).HasColumnType("jsonb");
            builder.Property(e => e.ResponseMetaJson).HasColumnType("jsonb");
            builder.Property(e => e.ErrorMessage).HasColumnType("text");
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => new { e.TenantId, e.WarehouseId, e.CreatedAt });
            builder.HasIndex(e => e.WorkflowRunId);
            builder.HasIndex(e => e.SessionId);
            builder.HasIndex(e => new { e.ProviderCode, e.ModelName });
        });
    }

    private static void ConfigureWorkflowCheckpoints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowCheckpoint>(builder =>
        {
            builder.ToTable("workflow_checkpoints");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.SessionId).HasMaxLength(128).IsRequired();
            builder.Property(e => e.ParentCheckpointId).HasMaxLength(128);
            builder.Property(e => e.CheckpointData).HasColumnType("jsonb").IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.HasIndex(e => e.SessionId);
            builder.HasIndex(e => new { e.SessionId, e.CreatedAt });
            builder.HasIndex(e => e.ParentCheckpointId);
        });
    }
}
