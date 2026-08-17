using AppEducativa.Api.Models.Institutions;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Data;

/// <summary>Colegio, período y un curso por nivel para que el picker de nómina no quede vacío.</summary>
public static class DemoSchoolSeed
{
    public static readonly Guid InstitutionId = ChileanCurriculumCatalogSeed.StableId("school:demo");
    public static readonly Guid PeriodId = ChileanCurriculumCatalogSeed.StableId("period:2026");

    public static void Ensure(AppEducativaDbContext db)
    {
        var institution = db.EducationalInstitutions
            .Where(i => i.IsActive && !i.IsDeleted)
            .OrderBy(i => i.CreatedAt)
            .FirstOrDefault();
        if (institution is null)
        {
            institution = new EducationalInstitution
            {
                Id = InstitutionId,
                Name = "Colegio Demo ProfeAsistente",
                ShortName = "Demo",
                Rbd = "00000-0",
                InstitutionType = EducationalInstitutionType.Municipal,
                Commune = "Santiago",
                Region = "Metropolitana",
                IsActive = true
            };
            db.EducationalInstitutions.Add(institution);
        }

        var period = db.AcademicPeriods.FirstOrDefault(p => p.Id == PeriodId || (p.InstitutionId == institution.Id && p.Year == 2026));
        if (period is null)
        {
            period = new AcademicPeriod
            {
                Id = PeriodId,
                InstitutionId = institution.Id,
                Name = "Año escolar 2026",
                Year = 2026,
                StartDate = new DateOnly(2026, 3, 2),
                EndDate = new DateOnly(2026, 12, 18),
                Status = AcademicPeriodStatus.Active,
                IsCurrent = true
            };
            db.AcademicPeriods.Add(period);
        }
        else
        {
            period.IsCurrent = true;
            if (period.Status == AcademicPeriodStatus.Draft)
                period.Status = AcademicPeriodStatus.Active;
        }

        var adminIds = db.Users
            .Where(u => u.UserName == "admin" || u.Email == "admin@appeducativa.local")
            .Select(u => u.Id)
            .ToList();
        foreach (var userId in adminIds)
        {
            if (db.InstitutionMemberships.Any(m => m.InstitutionId == institution.Id && m.UserId == userId && !m.IsDeleted))
                continue;
            db.InstitutionMemberships.Add(new InstitutionMembership
            {
                Id = ChileanCurriculumCatalogSeed.StableId("member:" + userId.ToString("N")),
                InstitutionId = institution.Id,
                UserId = userId,
                Role = ApplicationRole.SystemAdministrator,
                IsActive = true
            });
        }

        foreach (var nivel in db.Niveles.AsNoTracking().OrderBy(n => n.Orden).ToList())
        {
            var courseId = ChileanCurriculumCatalogSeed.StableId($"course:{nivel.Codigo}:A");
            if (db.SchoolCourses.Any(c => c.Id == courseId || (c.InstitutionId == institution.Id && c.LevelId == nivel.Id && c.Section == "A" && !c.IsDeleted)))
                continue;
            db.SchoolCourses.Add(new SchoolCourse
            {
                Id = courseId,
                InstitutionId = institution.Id,
                AcademicPeriodId = period.Id,
                LevelId = nivel.Id,
                Name = nivel.Nombre,
                Section = "A",
                DisplayName = $"{nivel.Nombre} A",
                Capacity = 40,
                IsActive = true
            });
        }
    }
}
