namespace WmsAi.Contracts.Errors;

public static class ErrorCodes
{
    public const string AuthUnauthorized = "AUTH_UNAUTHORIZED";
    public const string AuthForbidden = "AUTH_FORBIDDEN";
    public const string RequestInvalid = "REQUEST_INVALID";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ConflictDuplicatedRequest = "CONFLICT_DUPLICATED_REQUEST";

    public const string InboundNoticeNotFound = "INBOUND_NOTICE_NOT_FOUND";
    public const string QcTaskNotFound = "QC_TASK_NOT_FOUND";
    public const string QcTaskStatusInvalid = "QC_TASK_STATUS_INVALID";
    public const string EvidenceIncomplete = "EVIDENCE_INCOMPLETE";
    public const string DecisionAlreadyFinalized = "DECISION_ALREADY_FINALIZED";

    public const string AiSessionNotFound = "AI_SESSION_NOT_FOUND";
    public const string AiCheckpointNotFound = "AI_CHECKPOINT_NOT_FOUND";
    public const string AiModelProfileMissing = "AI_MODEL_PROFILE_MISSING";
    public const string AiModelRouteFailed = "AI_MODEL_ROUTE_FAILED";
    public const string AiSuggestionInvalid = "AI_SUGGESTION_INVALID";
}
