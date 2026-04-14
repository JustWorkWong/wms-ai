using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WmsAi.AiGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_inspection_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QcTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelConfigSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResultSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_inspection_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_model_providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ApiVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AuthMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CredentialRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_model_providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_routing_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SceneCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RoutingRulesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_routing_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maf_model_call_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowStepRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestTokens = table.Column<int>(type: "integer", nullable: false),
                    ResponseTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    FinishReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestMetaJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseMetaJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_model_call_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maf_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SessionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BusinessObjectType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BusinessObjectId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AgentSessionJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastCheckpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maf_tool_call_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowStepRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MembershipId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CallType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ToolName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: true),
                    OutputJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_tool_call_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maf_workflow_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AgentProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MembershipId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserInput = table.Column<string>(type: "text", nullable: true),
                    RoutingJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExecutionContextJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CurrentNode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_workflow_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParentCheckpointId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CheckpointData = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_checkpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_suggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QcTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AiInspectionRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_suggestions_ai_inspection_runs_AiInspectionRunId",
                        column: x => x.AiInspectionRunId,
                        principalTable: "ai_inspection_runs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ai_model_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SceneCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    TopP = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    RetryPolicyJson = table.Column<string>(type: "jsonb", nullable: true),
                    PromptAssetVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_model_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_model_profiles_ai_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ai_model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maf_checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckpointName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowStepRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    SummarySnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cursor = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maf_checkpoints_maf_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "maf_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maf_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentText = table.Column<string>(type: "text", nullable: true),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsSummary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maf_messages_maf_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "maf_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maf_summary_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryText = table.Column<string>(type: "text", nullable: false),
                    EvidenceRefsJson = table.Column<string>(type: "jsonb", nullable: true),
                    MessageRangeJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_summary_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maf_summary_snapshots_maf_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "maf_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maf_workflow_step_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    NodeName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AgentProfileCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StepKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    InputJson = table.Column<string>(type: "jsonb", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maf_workflow_step_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maf_workflow_step_runs_maf_workflow_runs_WorkflowRunId",
                        column: x => x.WorkflowRunId,
                        principalTable: "maf_workflow_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_inspection_runs_QcTaskId",
                table: "ai_inspection_runs",
                column: "QcTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_inspection_runs_TenantId_WarehouseId_Status",
                table: "ai_inspection_runs",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_inspection_runs_WorkflowRunId",
                table: "ai_inspection_runs",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_model_profiles_ProfileCode",
                table: "ai_model_profiles",
                column: "ProfileCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_model_profiles_ProviderId",
                table: "ai_model_profiles",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_model_profiles_SceneCode_IsActive",
                table: "ai_model_profiles",
                columns: new[] { "SceneCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_model_providers_ProviderCode",
                table: "ai_model_providers",
                column: "ProviderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_routing_policies_TenantId_SceneCode_Priority",
                table: "ai_routing_policies",
                columns: new[] { "TenantId", "SceneCode", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_suggestions_AiInspectionRunId",
                table: "ai_suggestions",
                column: "AiInspectionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_suggestions_TenantId_QcTaskId",
                table: "ai_suggestions",
                columns: new[] { "TenantId", "QcTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_checkpoints_SessionId_CheckpointName",
                table: "maf_checkpoints",
                columns: new[] { "SessionId", "CheckpointName" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_messages_SessionId_Sequence",
                table: "maf_messages",
                columns: new[] { "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maf_model_call_logs_ProviderCode_ModelName",
                table: "maf_model_call_logs",
                columns: new[] { "ProviderCode", "ModelName" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_model_call_logs_SessionId",
                table: "maf_model_call_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_maf_model_call_logs_TenantId_WarehouseId_CreatedAt",
                table: "maf_model_call_logs",
                columns: new[] { "TenantId", "WarehouseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_model_call_logs_WorkflowRunId",
                table: "maf_model_call_logs",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_maf_sessions_TenantId_BusinessObjectType_BusinessObjectId",
                table: "maf_sessions",
                columns: new[] { "TenantId", "BusinessObjectType", "BusinessObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_sessions_TenantId_WarehouseId_Status",
                table: "maf_sessions",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_sessions_UserId",
                table: "maf_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_maf_summary_snapshots_SessionId",
                table: "maf_summary_snapshots",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_maf_tool_call_logs_SessionId",
                table: "maf_tool_call_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_maf_tool_call_logs_TenantId_WarehouseId_CreatedAt",
                table: "maf_tool_call_logs",
                columns: new[] { "TenantId", "WarehouseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_tool_call_logs_WorkflowRunId",
                table: "maf_tool_call_logs",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_maf_workflow_runs_CreatedAt",
                table: "maf_workflow_runs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_maf_workflow_runs_TenantId_WarehouseId_Status",
                table: "maf_workflow_runs",
                columns: new[] { "TenantId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_workflow_runs_WorkflowName_Status",
                table: "maf_workflow_runs",
                columns: new[] { "WorkflowName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_maf_workflow_step_runs_WorkflowRunId_Sequence",
                table: "maf_workflow_step_runs",
                columns: new[] { "WorkflowRunId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_checkpoints_ParentCheckpointId",
                table: "workflow_checkpoints",
                column: "ParentCheckpointId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_checkpoints_SessionId_CreatedAt",
                table: "workflow_checkpoints",
                columns: new[] { "SessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_model_profiles");

            migrationBuilder.DropTable(
                name: "ai_routing_policies");

            migrationBuilder.DropTable(
                name: "ai_suggestions");

            migrationBuilder.DropTable(
                name: "maf_checkpoints");

            migrationBuilder.DropTable(
                name: "maf_messages");

            migrationBuilder.DropTable(
                name: "maf_model_call_logs");

            migrationBuilder.DropTable(
                name: "maf_summary_snapshots");

            migrationBuilder.DropTable(
                name: "maf_tool_call_logs");

            migrationBuilder.DropTable(
                name: "maf_workflow_step_runs");

            migrationBuilder.DropTable(
                name: "workflow_checkpoints");

            migrationBuilder.DropTable(
                name: "ai_model_providers");

            migrationBuilder.DropTable(
                name: "ai_inspection_runs");

            migrationBuilder.DropTable(
                name: "maf_sessions");

            migrationBuilder.DropTable(
                name: "maf_workflow_runs");
        }
    }
}
