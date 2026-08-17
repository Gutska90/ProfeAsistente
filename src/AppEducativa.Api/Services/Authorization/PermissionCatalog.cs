using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Security;

namespace AppEducativa.Api.Services.Authorization;

public static class PermissionCatalog
{
    private static readonly Dictionary<ApplicationRole, string[]> Map = new()
    {
        [ApplicationRole.SystemAdministrator] = AppPermissions.All.ToArray(),
        [ApplicationRole.CurriculumAdministrator] =
        [
            AppPermissions.CurriculumView, AppPermissions.CurriculumImport, AppPermissions.CurriculumReview,
            AppPermissions.CurriculumApprove, AppPermissions.CurriculumPublish,
            AppPermissions.PlanningViewInstitution, AppPermissions.MaterialsExport, AppPermissions.AuditView
        ],
        [ApplicationRole.SchoolAdministrator] =
        [
            AppPermissions.InstitutionsView, AppPermissions.InstitutionsUpdate,
            AppPermissions.CoursesView, AppPermissions.CoursesCreate, AppPermissions.CoursesUpdate,
            AppPermissions.CoursesArchive, AppPermissions.CoursesAssignTeachers,
            AppPermissions.PlanningViewInstitution, AppPermissions.PlanningCreate, AppPermissions.PlanningUpdateAny,
            AppPermissions.PlanningReview, AppPermissions.MaterialsExport, AppPermissions.AuditView,
            AppPermissions.UsersView,
            AppPermissions.ClassroomView, AppPermissions.ClassroomManageStudents, AppPermissions.ClassroomAttendance,
            AppPermissions.ClassroomEvaluate, AppPermissions.ClassroomSupportPlans
        ],
        [ApplicationRole.Teacher] =
        [
            AppPermissions.CoursesView, AppPermissions.PlanningViewOwn, AppPermissions.PlanningCreate,
            AppPermissions.PlanningUpdateOwn, AppPermissions.PlanningDeleteOwn,
            AppPermissions.MaterialsGenerate, AppPermissions.MaterialsEditOwn, AppPermissions.MaterialsFinalize,
            AppPermissions.MaterialsExport, AppPermissions.CurriculumView,
            AppPermissions.ClassroomView, AppPermissions.ClassroomManageStudents, AppPermissions.ClassroomAttendance,
            AppPermissions.ClassroomEvaluate, AppPermissions.ClassroomSupportPlans
        ],
        [ApplicationRole.Reviewer] =
        [
            AppPermissions.PlanningViewInstitution, AppPermissions.PlanningReview,
            AppPermissions.MaterialsReview, AppPermissions.CurriculumView, AppPermissions.CoursesView
        ],
        [ApplicationRole.ReadOnly] =
        [
            AppPermissions.PlanningViewOwn, AppPermissions.CoursesView, AppPermissions.CurriculumView,
            AppPermissions.InstitutionsView
        ]
    };

    public static IReadOnlyList<string> ForRoles(IEnumerable<string> roleNames)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in roleNames)
        {
            if (!Enum.TryParse<ApplicationRole>(name, true, out var role)) continue;
            foreach (var p in Map[role]) set.Add(p);
        }
        return set.OrderBy(x => x).ToList();
    }

    public static IReadOnlyList<string> ForMembershipRole(ApplicationRole role) => Map[role];
}
