namespace AppEducativa.Shared.Enums;

public enum ApplicationRole
{
    SystemAdministrator = 0,
    CurriculumAdministrator = 1,
    SchoolAdministrator = 2,
    Teacher = 3,
    Reviewer = 4,
    ReadOnly = 5
}

public enum EducationalInstitutionType
{
    Municipal = 0,
    SubsidizedPrivate = 1,
    Private = 2,
    PublicLocalService = 3,
    TechnicalProfessional = 4,
    Other = 5
}

public enum AcademicPeriodStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2,
    Archived = 3
}

public enum TeacherAssignmentType
{
    PrimaryTeacher = 0,
    CoTeacher = 1,
    Substitute = 2,
    Reviewer = 3,
    Assistant = 4
}

public enum PlanningVisibility
{
    Private = 0,
    CourseTeachers = 1,
    Institution = 2,
    SharedByLink = 3
}

public enum AttendanceStatus
{
    Present = 0,
    Absent = 1,
    Late = 2,
    JustifiedAbsence = 3,
    Withdrawn = 4
}

public enum SpecialEducationalNeedType
{
    None = 0,
    Permanent = 1,
    Transitory = 2,
    Gifted = 3
}

public enum SupportPlanType
{
    ClassroomDiversification = 0,
    Pie = 1,
    Decreto83Access = 2,
    Decreto83Objectives = 3,
    IndividualSupport = 4
}

public enum DuaPrinciple
{
    Engagement = 0,
    Representation = 1,
    ActionAndExpression = 2
}

public enum EvaluationPurpose
{
    Diagnostic = 0,
    Formative = 1,
    Summative = 2
}

public enum EnrollmentStatus
{
    Active = 0,
    Withdrawn = 1,
    Transferred = 2
}
