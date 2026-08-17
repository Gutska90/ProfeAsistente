using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Identity;
using ProfeAsistente.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Auth;

public static class IdentityBootstrap
{
    public static async Task EnsureRolesAndAdminAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRoleEntity>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ProfeAsistenteDbContext>();

        foreach (var role in Enum.GetNames<ApplicationRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRoleEntity(role) { Id = Guid.NewGuid() });
        }

        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (await userManager.Users.AnyAsync())
        {
            // En Development + Demo, habilitar login de prueba sin forzar cambio de contraseña.
            var demoEnabled = config.GetValue("Demo:Enabled", false)
                              || config.GetValue("Curriculum:IncludeDemoData", false);
            if (env.IsDevelopment() && demoEnabled)
            {
                var demo = await userManager.FindByNameAsync("admin");
                if (demo is not null && demo.MustChangePassword)
                {
                    demo.MustChangePassword = false;
                    await userManager.UpdateAsync(demo);
                    logger.LogInformation("Usuario demo admin: MustChangePassword desactivado.");
                }
            }
            return;
        }

        var userName = Environment.GetEnvironmentVariable("PROFEASISTENTE_ADMIN_USERNAME");
        var email = Environment.GetEnvironmentVariable("PROFEASISTENTE_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("PROFEASISTENTE_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException(
                    "Primera ejecución: configure PROFEASISTENTE_ADMIN_USERNAME, PROFEASISTENTE_ADMIN_EMAIL y PROFEASISTENTE_ADMIN_PASSWORD.");

            var demoEnabled = config.GetValue("Demo:Enabled", false)
                              || config.GetValue("Curriculum:IncludeDemoData", false);
            if (!demoEnabled)
                throw new InvalidOperationException(
                    "Development sin Demo:Enabled: configure PROFEASISTENTE_ADMIN_* o active Demo:Enabled.");

            userName ??= "admin";
            email ??= "admin@profeasistente.local";
            password ??= "Admin!Pass123";
            logger.LogWarning("Creando administrador de desarrollo (Demo:Enabled). Credenciales de prueba: admin / Admin!Pass123");
        }

        if (password.Length < 10)
            throw new InvalidOperationException("La contraseña del administrador inicial no cumple el mínimo de 10 caracteres.");

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "Sistema",
            DisplayName = "Administrador",
            // En Development no forzar cambio inmediato; en otros entornos sí.
            MustChangePassword = !env.IsDevelopment(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("No se pudo crear el administrador inicial: " +
                                                string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(admin, nameof(ApplicationRole.SystemAdministrator));
        db.TeacherProfiles.Add(new Models.Institutions.TeacherProfile { Id = Guid.NewGuid(), UserId = admin.Id });
        await db.SaveChangesAsync();
        logger.LogInformation("Usuario administrador inicial creado: {UserName}", admin.UserName);
    }
}
