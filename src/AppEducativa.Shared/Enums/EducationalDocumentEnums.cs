namespace AppEducativa.Shared.Enums;

public enum EducationalDocumentType
{
    LearningGuide = 0,
    Exercises = 1,
    Assessment = 2
}

public enum EducationalDocumentStatus
{
    Draft = 0,
    UnderReview = 1,
    Reviewed = 2,
    Final = 3,
    Archived = 4,
    Outdated = 5
}

public enum EducationalItemType
{
    MultipleChoice = 0,
    TrueFalse = 1,
    ShortAnswer = 2,
    OpenResponse = 3,
    Matching = 4,
    Completion = 5,
    ProblemSolving = 6,
    PracticalActivity = 7,
    Reflection = 8
}

public enum ItemDifficulty
{
    Basic = 0,
    Intermediate = 1,
    Advanced = 2
}

public enum AiDocumentGenerationType
{
    CompleteDocument = 0,
    SingleItem = 1
}
