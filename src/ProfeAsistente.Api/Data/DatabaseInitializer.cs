using Microsoft.EntityFrameworkCore;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProfeAsistenteDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        try
        {
            var cs = config.GetConnectionString("DefaultConnection") ?? "Data Source=profeasistente.db";
            var dataSource = cs.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
            var dbPath = Path.IsPathRooted(dataSource)
                ? dataSource
                : Path.GetFullPath(Path.Combine(env.ContentRootPath, dataSource));
            logger.LogInformation("SQLite: {DbPath}", dbPath);

            // Never delete an existing user database automatically. Legacy databases must be backed up
            // and migrated explicitly; they may contain planificaciones or reviewed official content.
            if (File.Exists(dbPath) && !await HasMigrationsHistoryAsync(db))
            {
                var hasPlans = await db.Planificaciones.AnyAsync();
                var hasOfficial = await db.ObjetivosAprendizaje.AnyAsync(o => o.EsContenidoOficial &&
                    o.EstadoRevision == EstadoRevision.Aprobado);
                if (hasPlans || hasOfficial)
                    logger.LogWarning("Base legada protegida: contiene planificaciones o currículum oficial aprobado.");
                else
                    logger.LogWarning("Base legada sin historial de migraciones; no se eliminará automáticamente.");
            }

            await db.Database.MigrateAsync();
            logger.LogInformation("Migraciones EF aplicadas.");

            await Services.Auth.IdentityBootstrap.EnsureRolesAndAdminAsync(services, logger);

            try
            {
                var orchestrator = scope.ServiceProvider.GetRequiredService<Services.Curriculum.OfficialCurriculumImportOrchestrator>();
                await orchestrator.ReloadSourcesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudieron recargar fuentes curriculares al iniciar.");
            }

            var demo = config.GetSection(Configuration.DemoOptions.SectionName).Get<Configuration.DemoOptions>()
                       ?? new Configuration.DemoOptions();
            // Compat: Curriculum:IncludeDemoData sigue funcionando en Development si Demo:Enabled no está.
            var includeDemoFlag = config.GetValue("Curriculum:IncludeDemoData", false);
            var includeDemo = env.IsDevelopment() && (demo.Enabled || includeDemoFlag);
            if (!env.IsDevelopment() && (demo.Enabled || includeDemoFlag))
                logger.LogWarning("Demo/seed ignorado fuera de Development (Demo:Enabled / Curriculum:IncludeDemoData).");

            var hasCurriculum = await db.ObjetivosAprendizaje.AnyAsync();
            if (includeDemo && !hasCurriculum)
            {
                DemoCurriculumSeed.Seed(db);
                await db.SaveChangesAsync();
                logger.LogInformation(
                    "Seed demostrativo cargado (FuenteTipo={Fuente}, EsContenidoOficial=false).",
                    DemoCurriculumSeed.FuenteTipo);
            }
            else if (!includeDemo)
            {
                logger.LogInformation("Seed demostrativo deshabilitado (requiere Development + Demo:Enabled).");
            }

            if (includeDemo)
            {
                ChileanCurriculumCatalogSeed.Ensure(db);
                await db.SaveChangesAsync();
                DemoSchoolSeed.Ensure(db);
                await db.SaveChangesAsync();
                logger.LogInformation(
                    "Catálogo escolar chileno asegurado (niveles NT1–4° medio, asignaturas y unidades plantilla).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo la inicialización de la base de datos.");
            throw new InvalidOperationException(
                "No se pudo inicializar la base de datos ProfeAsistente. Revise la cadena de conexión y las migraciones.",
                ex);
        }
    }

    private static async Task<bool> HasMigrationsHistoryAsync(ProfeAsistenteDbContext db)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
        catch
        {
            return false;
        }
    }
}
