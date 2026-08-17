namespace ProfeAsistente.Shared.Security;

public static class AppPermissions
{
    public const string UsersView = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersUpdate = "Users.Update";
    public const string UsersDisable = "Users.Disable";
    public const string UsersAssignRoles = "Users.AssignRoles";

    public const string InstitutionsView = "Institutions.View";
    public const string InstitutionsCreate = "Institutions.Create";
    public const string InstitutionsUpdate = "Institutions.Update";
    public const string InstitutionsDelete = "Institutions.Delete";

    public const string CoursesView = "Courses.View";
    public const string CoursesCreate = "Courses.Create";
    public const string CoursesUpdate = "Courses.Update";
    public const string CoursesArchive = "Courses.Archive";
    public const string CoursesAssignTeachers = "Courses.AssignTeachers";

    public const string PlanningViewOwn = "Planning.ViewOwn";
    public const string PlanningViewInstitution = "Planning.ViewInstitution";
    public const string PlanningCreate = "Planning.Create";
    public const string PlanningUpdateOwn = "Planning.UpdateOwn";
    public const string PlanningUpdateAny = "Planning.UpdateAny";
    public const string PlanningDeleteOwn = "Planning.DeleteOwn";
    public const string PlanningDeleteAny = "Planning.DeleteAny";
    public const string PlanningReview = "Planning.Review";
    public const string PlanningPublish = "Planning.Publish";

    public const string CurriculumView = "Curriculum.View";
    public const string CurriculumImport = "Curriculum.Import";
    public const string CurriculumReview = "Curriculum.Review";
    public const string CurriculumApprove = "Curriculum.Approve";
    public const string CurriculumPublish = "Curriculum.Publish";

    public const string MaterialsGenerate = "Materials.Generate";
    public const string MaterialsEditOwn = "Materials.EditOwn";
    public const string MaterialsEditAny = "Materials.EditAny";
    public const string MaterialsReview = "Materials.Review";
    public const string MaterialsFinalize = "Materials.Finalize";
    public const string MaterialsExport = "Materials.Export";

    public const string AuditView = "Audit.View";
    public const string SystemConfigure = "System.Configure";

    public const string ClassroomView = "Classroom.View";
    public const string ClassroomManageStudents = "Classroom.ManageStudents";
    public const string ClassroomAttendance = "Classroom.Attendance";
    public const string ClassroomEvaluate = "Classroom.Evaluate";
    public const string ClassroomSupportPlans = "Classroom.SupportPlans";

    public static IReadOnlyList<string> All { get; } =
    [
        UsersView, UsersCreate, UsersUpdate, UsersDisable, UsersAssignRoles,
        InstitutionsView, InstitutionsCreate, InstitutionsUpdate, InstitutionsDelete,
        CoursesView, CoursesCreate, CoursesUpdate, CoursesArchive, CoursesAssignTeachers,
        PlanningViewOwn, PlanningViewInstitution, PlanningCreate, PlanningUpdateOwn, PlanningUpdateAny,
        PlanningDeleteOwn, PlanningDeleteAny, PlanningReview, PlanningPublish,
        CurriculumView, CurriculumImport, CurriculumReview, CurriculumApprove, CurriculumPublish,
        MaterialsGenerate, MaterialsEditOwn, MaterialsEditAny, MaterialsReview, MaterialsFinalize, MaterialsExport,
        AuditView, SystemConfigure,
        ClassroomView, ClassroomManageStudents, ClassroomAttendance, ClassroomEvaluate, ClassroomSupportPlans
    ];
}

public static class AppPolicies
{
    public const string RequireSystemAdministrator = "RequireSystemAdministrator";
    public const string RequireCurriculumAdministrator = "RequireCurriculumAdministrator";
    public const string RequireSchoolAdministrator = "RequireSchoolAdministrator";
    public const string RequireTeacher = "RequireTeacher";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageCurriculum = "CanManageCurriculum";
    public const string CanCreatePlanning = "CanCreatePlanning";
    public const string CanReviewPlanning = "CanReviewPlanning";
    public const string CanExportMaterials = "CanExportMaterials";
    public const string CanViewAudit = "CanViewAudit";
}
