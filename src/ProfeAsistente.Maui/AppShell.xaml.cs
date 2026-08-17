using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Security;
using ProfeAsistente.Maui.Views;
using ProfeAsistente.Maui.Views.Admin;
using ProfeAsistente.Maui.Views.Admin.Courses;
using ProfeAsistente.Maui.Views.Admin.CurriculumReview;
using ProfeAsistente.Maui.Views.Admin.Institutions;
using ProfeAsistente.Maui.Views.Admin.Users;
using ProfeAsistente.Maui.Views.Auth;
using ProfeAsistente.Maui.Views.Classroom;
using ProfeAsistente.Maui.Views.Documents;
using ProfeAsistente.Maui.Views.Exports;
using ProfeAsistente.Maui.Views.Institutions;
using ProfeAsistente.Maui.Views.Planning;

namespace ProfeAsistente.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("asistencia", typeof(AttendancePage));
        Routing.RegisterRoute("evaluacionClase", typeof(ClassAssessmentPage));
        Routing.RegisterRoute("changePassword", typeof(ChangePasswordPage));
        Routing.RegisterRoute("forgotPassword", typeof(ForgotPasswordPage));
        Routing.RegisterRoute("institutionSelector", typeof(InstitutionSelectorPage));
        Routing.RegisterRoute("sessions", typeof(SessionListPage));
        Routing.RegisterRoute("userDetail", typeof(UserDetailPage));
        Routing.RegisterRoute("createUser", typeof(CreateUserPage));
        Routing.RegisterRoute("userRoles", typeof(UserRolesPage));
        Routing.RegisterRoute("institutionDetail", typeof(InstitutionDetailPage));
        Routing.RegisterRoute("institutionMembers", typeof(InstitutionMembersPage));
        Routing.RegisterRoute("courseDetail", typeof(CourseDetailPage));
        Routing.RegisterRoute("courseTeachers", typeof(CourseTeachersPage));
        Routing.RegisterRoute("nuevaPlanificacion", typeof(NuevaPlanificacionPage));
        Routing.RegisterRoute("planificacionDetalle", typeof(PlanificacionDetallePage));
        Routing.RegisterRoute("claseDetalle", typeof(ClaseDetallePage));
        Routing.RegisterRoute("classStructureGeneration", typeof(ClassStructureGenerationPage));
        Routing.RegisterRoute("classStructureEditor", typeof(ClassStructureEditorPage));
        Routing.RegisterRoute("educationalDocuments", typeof(EducationalDocumentListPage));
        Routing.RegisterRoute("educationalDocumentGeneration", typeof(EducationalDocumentGenerationPage));
        Routing.RegisterRoute("educationalDocumentEditor", typeof(EducationalDocumentEditorPage));
        Routing.RegisterRoute("educationalItemEditor", typeof(EducationalItemEditorPage));
        Routing.RegisterRoute("assessmentSpecification", typeof(AssessmentSpecificationPage));
        Routing.RegisterRoute("educationalDocumentComparison", typeof(EducationalDocumentComparisonPage));
        Routing.RegisterRoute("exportOptions", typeof(ExportOptionsPage));
        Routing.RegisterRoute("exportProgress", typeof(ExportProgressPage));
        Routing.RegisterRoute("exportHistory", typeof(ExportHistoryPage));
        Routing.RegisterRoute("planningCalendar", typeof(PlanningCalendarPage));
        Routing.RegisterRoute("planningCalendarMonth", typeof(PlanningCalendarMonthPage));
        Routing.RegisterRoute("planningCalendarWeek", typeof(PlanningCalendarWeekPage));
        Routing.RegisterRoute("planningSchedule", typeof(PlanningScheduleConfigurationPage));
        Routing.RegisterRoute("planningSequence", typeof(PlanningSequenceGeneratorPage));
        Routing.RegisterRoute("planningCoverage", typeof(PlanningCoverageDashboardPage));
        Routing.RegisterRoute("planningAlerts", typeof(PlanningAlertsPage));
        Routing.RegisterRoute("planificaciones/planificacionDetalle", typeof(PlanificacionDetallePage));
        Routing.RegisterRoute("adminImportDetail", typeof(CurriculumImportDetailPage));
        Routing.RegisterRoute("adminImportPreview", typeof(CurriculumImportPreviewPage));
        Routing.RegisterRoute("adminReviewDashboard", typeof(CurriculumReviewDashboardPage));
        Routing.RegisterRoute("adminReviewUnits", typeof(CurriculumReviewUnitsPage));
        Routing.RegisterRoute("adminReviewObjectives", typeof(CurriculumReviewObjectivesPage));
        Routing.RegisterRoute("adminReviewObjectiveDetail", typeof(CurriculumReviewObjectiveDetailPage));
        Routing.RegisterRoute("adminReviewIssues", typeof(CurriculumReviewIssuesPage));
        Routing.RegisterRoute("adminReviewChanges", typeof(CurriculumReviewChangesPage));
        Routing.RegisterRoute("adminReviewDiff", typeof(CurriculumReviewDiffPage));
        Routing.RegisterRoute("adminReviewComments", typeof(CurriculumReviewCommentsPage));
    }

    public void WireSessionExpired(IAuthenticationService auth)
    {
        auth.SessionExpired += async (_, _) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ApplyMenu(auth);
                await DisplayAlert("Sesión", "Tu sesión expiró. Inicia sesión nuevamente.", "OK");
                await GoToAsync("//login");
            });
        };
    }

    public static void ApplyMenuForCurrentUser(IAuthenticationService auth)
    {
        if (Current is AppShell shell)
            shell.ApplyMenu(auth);
    }

    public void ApplyMenu(IAuthenticationService auth)
    {
        var signedIn = !string.IsNullOrWhiteSpace(auth.AccessToken);
        bool Has(string permission) =>
            auth.Roles.Contains(nameof(ApplicationRole.SystemAdministrator))
            || auth.Permissions.Contains(permission);

        FlyoutHoy.FlyoutItemIsVisible = signedIn;
        FlyoutPlanificaciones.FlyoutItemIsVisible = signedIn
            && (Has(AppPermissions.PlanningViewOwn) || Has(AppPermissions.PlanningViewInstitution));
        FlyoutBiblioteca.FlyoutItemIsVisible = signedIn
            && (Has(AppPermissions.MaterialsEditOwn) || Has(AppPermissions.MaterialsGenerate)
                || Has(AppPermissions.PlanningViewOwn) || Has(AppPermissions.ClassroomView));
        FlyoutNuevaPlanificacion.FlyoutItemIsVisible = false;
        FlyoutNomina.FlyoutItemIsVisible = false;
        FlyoutPerfil.FlyoutItemIsVisible = signedIn;
        // Administración: fuera del MVP docente; solo roles con permiso explícito.
        FlyoutUsuarios.FlyoutItemIsVisible = signedIn && Has(AppPermissions.UsersView);
        FlyoutEstablecimientos.FlyoutItemIsVisible = signedIn && Has(AppPermissions.InstitutionsView);
        FlyoutCursos.FlyoutItemIsVisible = signedIn
            && (Has(AppPermissions.CoursesView) || Has(AppPermissions.ClassroomView));
        var curriculumAdmin = Has(AppPermissions.CurriculumImport)
            || Has(AppPermissions.CurriculumReview)
            || Has(AppPermissions.CurriculumApprove)
            || Has(AppPermissions.CurriculumPublish);
        FlyoutCurriculum.FlyoutItemIsVisible = signedIn && curriculumAdmin;
        FlyoutFuentes.FlyoutItemIsVisible = signedIn && Has(AppPermissions.CurriculumImport);
        FlyoutLotes.FlyoutItemIsVisible = signedIn && Has(AppPermissions.CurriculumImport);
    }
}
