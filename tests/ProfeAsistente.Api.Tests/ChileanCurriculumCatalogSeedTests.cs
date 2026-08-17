using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Tests;

public class ChileanCurriculumCatalogSeedTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-catalog-{Guid.NewGuid():N}.db");
    private readonly ProfeAsistenteDbContext _db;

    public ChileanCurriculumCatalogSeedTests()
    {
        var options = new DbContextOptionsBuilder<ProfeAsistenteDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new ProfeAsistenteDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        ChileanCurriculumCatalogSeed.Ensure(_db);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Catalogo_IncluyeTodosLosNivelesEscolares()
    {
        var repo = new CurriculumRepository(_db);
        var niveles = await repo.GetNivelesAsync();
        Assert.Contains(niveles, n => n.Codigo == "NT1");
        Assert.Contains(niveles, n => n.Codigo == "4B");
        Assert.Contains(niveles, n => n.Codigo == "8B");
        Assert.Contains(niveles, n => n.Codigo == "1M");
        Assert.Contains(niveles, n => n.Codigo == "4M");
        Assert.True(niveles.Count >= 14);
    }

    [Fact]
    public async Task CuartoBasico_TieneVariasAsignaturas()
    {
        var repo = new CurriculumRepository(_db);
        var nivel = (await repo.GetNivelesAsync()).Single(n => n.Codigo == "4B");
        var asignaturas = await repo.GetAsignaturasAsync(nivel.Id);
        Assert.Contains(asignaturas, a => a.Codigo == "MAT");
        Assert.Contains(asignaturas, a => a.Codigo == "LEN");
        Assert.Contains(asignaturas, a => a.Codigo == "CN");
        Assert.True(asignaturas.Count >= 8);
    }

    [Fact]
    public async Task NoPisaLaUnidadDemoDeMatematica4B()
    {
        var repo = new CurriculumRepository(_db);
        var unidades = await repo.GetUnidadesAsync(DemoCurriculumSeed.NivelAsignaturaId);
        Assert.Contains(unidades, u => u.Id == DemoCurriculumSeed.UnidadId);
    }

    [Fact]
    public async Task LenguajeDePrimeroBasico_TieneUnidadesPlantilla()
    {
        var repo = new CurriculumRepository(_db);
        var nivel = (await repo.GetNivelesAsync()).Single(n => n.Codigo == "1B");
        var len = (await repo.GetAsignaturasAsync(nivel.Id)).Single(a => a.Codigo == "LEN");
        var unidades = await repo.GetUnidadesAsync(len.Id);
        Assert.True(unidades.Count >= 4);
        var oas = await repo.GetObjetivosPorUnidadAsync(unidades[0].Id);
        Assert.NotEmpty(oas);
        Assert.All(oas, o => Assert.False(o.EsContenidoOficial));
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
    }
}
