namespace ProfeAsistente.Api.Services.AI;

/// <summary>Propósitos de generación para telemetría / precios (P11).</summary>
public static class AiGenerationPurposes
{
    public const string ClassPlan = "ClassPlan";
    public const string Guide = "Guide";
    public const string Exercises = "Exercises";
    public const string Assessment = "Assessment";
    public const string ExitTicket = "ExitTicket";
    public const string Reinforcement = "Reinforcement";
    public const string ItemRegenerate = "ItemRegenerate";
    public const string AdaptSimplify = "AdaptSimplify";
    public const string AdaptScaffold = "AdaptScaffold";
}

public static class PromptCatalog
{
    public const string ClassStructureId = "class-structure";
    public const string LearningGuideId = "learning-guide";
    public const string ExercisesId = "exercises";
    public const string AssessmentId = "assessment";

    public static (string PromptId, string PromptVersion) ForClassStructure(string configuredVersion)
        => (ClassStructureId, string.IsNullOrWhiteSpace(configuredVersion) ? "class-structure-v1" : configuredVersion);

    public static (string PromptId, string PromptVersion) ForDocument(Shared.Enums.EducationalDocumentType type)
        => type switch
        {
            Shared.Enums.EducationalDocumentType.LearningGuide => (LearningGuideId, "learning-guide-v1"),
            Shared.Enums.EducationalDocumentType.Exercises => (ExercisesId, "exercises-v1"),
            Shared.Enums.EducationalDocumentType.Assessment => (AssessmentId, "assessment-v1"),
            _ => ("document", "document-v1")
        };

    public static string PurposeForDocument(
        Shared.Enums.EducationalDocumentType type,
        string? teacherIntentHint = null)
    {
        if (!string.IsNullOrWhiteSpace(teacherIntentHint))
        {
            var h = teacherIntentHint.ToLowerInvariant();
            if (h.Contains("exit") || h.Contains("ticket")) return AiGenerationPurposes.ExitTicket;
            if (h.Contains("reinforce") || h.Contains("refuerzo")) return AiGenerationPurposes.Reinforcement;
            if (h.Contains("simplif")) return AiGenerationPurposes.AdaptSimplify;
            if (h.Contains("scaffold") || h.Contains("andamiaje")) return AiGenerationPurposes.AdaptScaffold;
        }

        return type switch
        {
            Shared.Enums.EducationalDocumentType.LearningGuide => AiGenerationPurposes.Guide,
            Shared.Enums.EducationalDocumentType.Exercises => AiGenerationPurposes.Exercises,
            Shared.Enums.EducationalDocumentType.Assessment => AiGenerationPurposes.Assessment,
            _ => type.ToString()
        };
    }
}
