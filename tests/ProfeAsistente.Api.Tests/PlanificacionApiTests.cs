using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Repositories;
using ProfeAsistente.Api.Services;
using ProfeAsistente.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Tests;

public class PlanificacionApiTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-tests-{Guid.NewGuid():N}.db");
    private readonly ProfeAsistenteDbContext _db;
    private readonly IPlanificacionService _planes;
    private readonly IClaseService _clases;
    private readonly ICurriculumRepository _curriculum;

    public PlanificacionApiTests()
    {
        var options = new DbContextOptionsBuilder<ProfeAsistenteDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new ProfeAsistenteDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        _db.SaveChanges();

        var planRepo = new PlanificacionRepository(_db);
        var claseRepo = new ClaseRepository(_db);
        _planes = new PlanificacionService(_db, planRepo, new ProfeAsistente.Api.Tests.TestDoubles.FakeCurrentUserService(), new ProfeAsistente.Api.Tests.TestDoubles.AllowAllResourceAuthorizationService());
        _clases = new ClaseService(_db, planRepo, claseRepo);
        _curriculum = new CurriculumRepository(_db);
    }

    [Fact]
    public async Task GetNiveles_ReturnsDemoLevel()
    {
        var niveles = await _curriculum.GetNivelesAsync();
        Assert.Contains(niveles, n => n.Codigo == "4B");
    }

    [Fact]
    public async Task GetObjetivos_DeUnidadDemo()
    {
        var oas = await _curriculum.GetObjetivosPorUnidadAsync(DemoCurriculumSeed.UnidadId);
        Assert.True(oas.Count >= 2);
        Assert.All(oas, o => Assert.False(o.EsContenidoOficial));
        Assert.All(oas, o => Assert.Equal(DemoCurriculumSeed.FuenteTipo, o.FuenteTipo));
    }

    [Fact]
    public async Task CrearPlanificacion_Valida()
    {
        var plan = await _planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Plan test",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 3, 31)
        });
        Assert.Equal("Plan test", plan.Nombre);
        Assert.Equal(DemoCurriculumSeed.UnidadId, plan.UnidadId);
    }

    [Fact]
    public async Task CrearPlanificacion_FechasInvalidas_Lanza()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Mal",
            FechaInicio = new DateOnly(2026, 4, 1),
            FechaFin = new DateOnly(2026, 3, 1)
        }));
    }

    [Fact]
    public async Task CrearClase_Valida()
    {
        var plan = await CrearPlanAsync();
        var clase = await _clases.CrearAsync(plan.Id, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            NivelBloom = "Recordar",
            Fecha = new DateOnly(2026, 3, 5)
        });
        Assert.Equal(1, clase.Numero);
        Assert.Equal("Recordar", clase.NivelBloom);
    }

    [Fact]
    public async Task CrearClase_FueraDeRango_Lanza()
    {
        var plan = await CrearPlanAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => _clases.CrearAsync(plan.Id, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            NivelBloom = "Aplicar",
            Fecha = new DateOnly(2026, 12, 1)
        }));
    }

    [Fact]
    public async Task CrearClase_OaAjeno_Lanza()
    {
        var plan = await CrearPlanAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => _clases.CrearAsync(plan.Id, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            NivelBloom = "Aplicar",
            Fecha = new DateOnly(2026, 3, 5)
        }));
    }

    private Task<PlanificacionDetalleDto> CrearPlanAsync() =>
        _planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = $"Plan {Guid.NewGuid():N}",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 3, 31)
        });

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            foreach (var s in new[] { "-shm", "-wal" })
            {
                var p = _dbPath + s;
                if (File.Exists(p)) File.Delete(p);
            }
        }
        catch { /* ignore */ }
    }
}
