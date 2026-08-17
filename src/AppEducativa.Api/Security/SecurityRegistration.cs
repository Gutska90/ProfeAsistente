using System.Text;
using System.Threading.RateLimiting;
using AppEducativa.Api.Configuration;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Identity;
using AppEducativa.Api.Services.Auth;
using AppEducativa.Api.Services.Classroom;
using AppEducativa.Api.Services.Authorization;
using AppEducativa.Api.Services.Institutions;
using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AppEducativa.Api.Security;

public static class SecurityRegistration
{
    public static void AddAppEducativaSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
        builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
        builder.Services.Configure<DevelopmentAuthenticationOptions>(builder.Configuration.GetSection(DevelopmentAuthenticationOptions.SectionName));

        var authOptions = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
                          ?? new AuthenticationOptions();
        var rateOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                          ?? new RateLimitingOptions();
        var devAuth = builder.Configuration.GetSection(DevelopmentAuthenticationOptions.SectionName).Get<DevelopmentAuthenticationOptions>()
                      ?? new DevelopmentAuthenticationOptions();

        if (devAuth.Enabled && builder.Environment.IsProduction())
            throw new InvalidOperationException("DevelopmentAuthentication no puede habilitarse en Production.");

        var jwtKey = Environment.GetEnvironmentVariable("APPEDUCATIVA_JWT_KEY");
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (!builder.Environment.IsDevelopment() || !authOptions.AllowDevelopmentSigningKey)
                throw new InvalidOperationException("Configure la variable de entorno APPEDUCATIVA_JWT_KEY.");
            jwtKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey.Length >= 32 ? jwtKey : jwtKey.PadRight(32, '0')));
        builder.Services.AddSingleton(signingKey);

        builder.Services
            .AddIdentity<ApplicationUser, ApplicationRoleEntity>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = authOptions.MaximumFailedAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(authOptions.LockoutMinutes);
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = authOptions.RequireConfirmedEmail;
            })
            .AddEntityFrameworkStores<AppEducativaDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidAudience = authOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        var isDev = builder.Environment.IsDevelopment();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CurriculumAdmin", policy =>
                policy.RequireAssertion(ctx =>
                {
                    if (isDev && ctx.User.Identity?.IsAuthenticated != true)
                        return true;
                    return ctx.User.HasClaim("CurriculumAdmin", "true")
                           || ctx.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))
                           || ctx.User.IsInRole(nameof(ApplicationRole.CurriculumAdministrator));
                }));

            options.AddPolicy(AppPolicies.RequireSystemAdministrator, p => p.RequireRole(nameof(ApplicationRole.SystemAdministrator)));
            options.AddPolicy(AppPolicies.RequireCurriculumAdministrator, p =>
                p.RequireRole(nameof(ApplicationRole.CurriculumAdministrator), nameof(ApplicationRole.SystemAdministrator)));
            options.AddPolicy(AppPolicies.RequireSchoolAdministrator, p =>
                p.RequireRole(nameof(ApplicationRole.SchoolAdministrator), nameof(ApplicationRole.SystemAdministrator)));
            options.AddPolicy(AppPolicies.RequireTeacher, p =>
                p.RequireRole(nameof(ApplicationRole.Teacher), nameof(ApplicationRole.SystemAdministrator), nameof(ApplicationRole.SchoolAdministrator)));
            options.AddPolicy(AppPolicies.CanManageUsers, p => p.RequireAssertion(c =>
                c.User.HasClaim("permission", AppPermissions.UsersCreate)
                || c.User.HasClaim("permission", AppPermissions.UsersView)
                || c.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))));
            options.AddPolicy(AppPolicies.CanManageCurriculum, p => p.RequireAssertion(c =>
                c.User.HasClaim("permission", AppPermissions.CurriculumImport)
                || c.User.IsInRole(nameof(ApplicationRole.CurriculumAdministrator))
                || c.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))));
            options.AddPolicy(AppPolicies.CanCreatePlanning, p => p.RequireAssertion(c =>
                c.User.HasClaim("permission", AppPermissions.PlanningCreate)
                || c.User.IsInRole(nameof(ApplicationRole.Teacher))
                || c.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))));
            options.AddPolicy(AppPolicies.CanReviewPlanning, p => p.RequireAssertion(c =>
                c.User.HasClaim("permission", AppPermissions.PlanningReview)
                || c.User.IsInRole(nameof(ApplicationRole.Reviewer))
                || c.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))));
            options.AddPolicy(AppPolicies.CanExportMaterials, p => p.RequireAssertion(c =>
                c.User.HasClaim("permission", AppPermissions.MaterialsExport)
                || c.User.IsInRole(nameof(ApplicationRole.Teacher))
                || c.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))));
            options.AddPolicy(AppPolicies.CanViewAudit, p => p.RequireAssertion(c =>
                c.User.HasClaim("permission", AppPermissions.AuditView)
                || c.User.IsInRole(nameof(ApplicationRole.SystemAdministrator))));
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = rateOptions.LoginRequestsPerMinute;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("refresh", opt =>
            {
                opt.PermitLimit = rateOptions.RefreshRequestsPerMinute;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("password-reset", opt =>
            {
                opt.PermitLimit = rateOptions.PasswordResetRequestsPerHour;
                opt.Window = TimeSpan.FromHours(1);
                opt.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        builder.Services.AddScoped<IInstitutionService, InstitutionService>();
        builder.Services.AddScoped<IUserAdminService, UserAdminService>();
        builder.Services.AddScoped<IClassroomService, ClassroomService>();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AppEducativa API",
                Version = "v1",
                Description = "API del planificador de clases con autenticación JWT."
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Bearer. Ejemplo: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}
