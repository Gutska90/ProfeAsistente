namespace AppEducativa.Shared.Enums;

public enum AiGenerationStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    RejectedByValidation = 5
}

public enum ClassStructureUiStatus
{
    None = 0,
    Generating = 1,
    Generated = 2,
    RequiresReview = 3,
    Reviewed = 4,
    Outdated = 5,
    Error = 6
}
