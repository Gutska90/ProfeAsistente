namespace ProfeAsistente.Shared.Enums;

public enum ExportDocumentType
{
    Planning = 0,
    ClassPlan = 1,
    LearningGuide = 2,
    Exercises = 3,
    Assessment = 4,
    AnswerKey = 5,
    SpecificationTable = 6,
    PlanningPackage = 7
}

public enum ExportAudience
{
    Student = 0,
    Teacher = 1,
    Administrative = 2
}

public enum ExportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Invalid = 5,
    Expired = 6
}
