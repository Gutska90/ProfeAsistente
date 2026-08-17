using ProfeAsistente.Maui.Configuration;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Maui.ViewModels;
using ProfeAsistente.Maui.ViewModels.Admin;
using ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;
using ProfeAsistente.Maui.ViewModels.Admin.Courses;
using ProfeAsistente.Maui.ViewModels.Admin.Institutions;
using ProfeAsistente.Maui.ViewModels.Admin.Users;
using ProfeAsistente.Maui.ViewModels.Auth;
using ProfeAsistente.Maui.ViewModels.Classroom;
using ProfeAsistente.Maui.ViewModels.Documents;
using ProfeAsistente.Maui.ViewModels.Exports;
using ProfeAsistente.Maui.ViewModels.Planning;
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
using Microsoft.Extensions.Logging;

namespace ProfeAsistente.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(ApiSettings.Default);
        builder.Services.AddSingleton<LocalApiLauncher>();
        builder.Services.AddSingleton<ITokenStorageService, SecureTokenStorageService>();

        builder.Services.AddSingleton<IAuthenticationService>(sp =>
        {
            var settings = sp.GetRequiredService<ApiSettings>();
            var http = new HttpClient
            {
                BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(120)
            };
            return new AuthenticationService(http, sp.GetRequiredService<ITokenStorageService>());
        });

        builder.Services.AddSingleton<AuthenticatedApiClientHandler>();
        builder.Services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<ApiSettings>();
            var handler = sp.GetRequiredService<AuthenticatedApiClientHandler>();
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(120)
            };
        });
        builder.Services.AddSingleton<IApiClient>(sp => new ApiClient(sp.GetRequiredService<HttpClient>()));
        builder.Services.AddSingleton<IOfflineSyncService, OfflineSyncService>();
        builder.Services.AddSingleton<IFileSaveService, FileSaveService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ChangePasswordViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<UserListViewModel>();
        builder.Services.AddTransient<UserDetailViewModel>();
        builder.Services.AddTransient<CreateUserViewModel>();
        builder.Services.AddTransient<UserRolesViewModel>();
        builder.Services.AddTransient<InstitutionListViewModel>();
        builder.Services.AddTransient<InstitutionDetailViewModel>();
        builder.Services.AddTransient<InstitutionMembersViewModel>();
        builder.Services.AddTransient<CourseListViewModel>();
        builder.Services.AddTransient<CourseDetailViewModel>();
        builder.Services.AddTransient<CourseTeachersViewModel>();

        builder.Services.AddTransient<TeacherDashboardViewModel>();
        builder.Services.AddTransient<CourseRosterViewModel>();
        builder.Services.AddTransient<AttendanceViewModel>();
        builder.Services.AddTransient<ClassAssessmentViewModel>();
        builder.Services.AddTransient<PlanificacionListViewModel>();
        builder.Services.AddTransient<NuevaPlanificacionViewModel>();
        builder.Services.AddTransient<PlanificacionDetalleViewModel>();
        builder.Services.AddTransient<ClaseDetalleViewModel>();
        builder.Services.AddTransient<ClassStructureGenerationViewModel>();
        builder.Services.AddTransient<ClassStructureEditorViewModel>();
        builder.Services.AddTransient<EducationalDocumentListViewModel>();
        builder.Services.AddTransient<MaterialLibraryViewModel>();
        builder.Services.AddTransient<EducationalDocumentGenerationViewModel>();
        builder.Services.AddTransient<EducationalDocumentEditorViewModel>();
        builder.Services.AddTransient<EducationalItemEditorViewModel>();
        builder.Services.AddTransient<AssessmentSpecificationViewModel>();
        builder.Services.AddTransient<EducationalDocumentComparisonViewModel>();
        builder.Services.AddTransient<ExportOptionsViewModel>();
        builder.Services.AddTransient<ExportProgressViewModel>();
        builder.Services.AddTransient<ExportHistoryViewModel>();
        builder.Services.AddTransient<PlanningCalendarViewModel>();
        builder.Services.AddTransient<PlanningCalendarMonthViewModel>();
        builder.Services.AddTransient<PlanningCalendarWeekViewModel>();
        builder.Services.AddTransient<PlanningScheduleConfigurationViewModel>();
        builder.Services.AddTransient<PlanningSequenceGeneratorViewModel>();
        builder.Services.AddTransient<PlanningCoverageDashboardViewModel>();
        builder.Services.AddTransient<PlanningAlertsViewModel>();
        builder.Services.AddTransient<AdministrarCurriculumViewModel>();
        builder.Services.AddTransient<CurriculumSourcesViewModel>();
        builder.Services.AddTransient<CurriculumImportsViewModel>();
        builder.Services.AddTransient<CurriculumImportDetailViewModel>();
        builder.Services.AddTransient<CurriculumImportPreviewViewModel>();
        builder.Services.AddTransient<CurriculumReviewDashboardViewModel>();
        builder.Services.AddTransient<CurriculumReviewUnitsViewModel>();
        builder.Services.AddTransient<CurriculumReviewObjectivesViewModel>();
        builder.Services.AddTransient<CurriculumReviewObjectiveDetailViewModel>();
        builder.Services.AddTransient<CurriculumReviewIssuesViewModel>();
        builder.Services.AddTransient<CurriculumReviewChangesViewModel>();
        builder.Services.AddTransient<CurriculumReviewDiffViewModel>();
        builder.Services.AddTransient<CurriculumReviewCommentsViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<TeacherDashboardPage>();
        builder.Services.AddTransient<CourseRosterPage>();
        builder.Services.AddTransient<AttendancePage>();
        builder.Services.AddTransient<ClassAssessmentPage>();
        builder.Services.AddTransient<ChangePasswordPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<UserProfilePage>();
        builder.Services.AddTransient<SessionListPage>();
        builder.Services.AddTransient<InstitutionSelectorPage>();
        builder.Services.AddTransient<UserListPage>();
        builder.Services.AddTransient<UserDetailPage>();
        builder.Services.AddTransient<CreateUserPage>();
        builder.Services.AddTransient<UserRolesPage>();
        builder.Services.AddTransient<InstitutionListPage>();
        builder.Services.AddTransient<InstitutionDetailPage>();
        builder.Services.AddTransient<InstitutionMembersPage>();
        builder.Services.AddTransient<CourseListPage>();
        builder.Services.AddTransient<CourseDetailPage>();
        builder.Services.AddTransient<CourseTeachersPage>();
        builder.Services.AddTransient<PlanificacionListPage>();
        builder.Services.AddTransient<NuevaPlanificacionPage>();
        builder.Services.AddTransient<PlanificacionDetallePage>();
        builder.Services.AddTransient<ClaseDetallePage>();
        builder.Services.AddTransient<ClassStructureGenerationPage>();
        builder.Services.AddTransient<ClassStructureEditorPage>();
        builder.Services.AddTransient<EducationalDocumentListPage>();
        builder.Services.AddTransient<MaterialLibraryPage>();
        builder.Services.AddTransient<EducationalDocumentGenerationPage>();
        builder.Services.AddTransient<EducationalDocumentEditorPage>();
        builder.Services.AddTransient<EducationalItemEditorPage>();
        builder.Services.AddTransient<AssessmentSpecificationPage>();
        builder.Services.AddTransient<EducationalDocumentComparisonPage>();
        builder.Services.AddTransient<ExportOptionsPage>();
        builder.Services.AddTransient<ExportProgressPage>();
        builder.Services.AddTransient<ExportHistoryPage>();
        builder.Services.AddTransient<PlanningCalendarPage>();
        builder.Services.AddTransient<PlanningCalendarMonthPage>();
        builder.Services.AddTransient<PlanningCalendarWeekPage>();
        builder.Services.AddTransient<PlanningScheduleConfigurationPage>();
        builder.Services.AddTransient<PlanningSequenceGeneratorPage>();
        builder.Services.AddTransient<PlanningCoverageDashboardPage>();
        builder.Services.AddTransient<PlanningAlertsPage>();
        builder.Services.AddTransient<AdministrarCurriculumPage>();
        builder.Services.AddTransient<CurriculumSourcesPage>();
        builder.Services.AddTransient<CurriculumImportsPage>();
        builder.Services.AddTransient<CurriculumImportDetailPage>();
        builder.Services.AddTransient<CurriculumImportPreviewPage>();
        builder.Services.AddTransient<CurriculumReviewDashboardPage>();
        builder.Services.AddTransient<CurriculumReviewUnitsPage>();
        builder.Services.AddTransient<CurriculumReviewObjectivesPage>();
        builder.Services.AddTransient<CurriculumReviewObjectiveDetailPage>();
        builder.Services.AddTransient<CurriculumReviewIssuesPage>();
        builder.Services.AddTransient<CurriculumReviewChangesPage>();
        builder.Services.AddTransient<CurriculumReviewDiffPage>();
        builder.Services.AddTransient<CurriculumReviewCommentsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
