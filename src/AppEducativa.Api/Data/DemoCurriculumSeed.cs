using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Data;

/// <summary>
/// Seed demostrativo (NO es contenido oficial MINEDUC). IDs estables para idempotencia.
/// </summary>
public static class DemoCurriculumSeed
{
    public const string FuenteTipo = "SeedDemostracion";

    public static readonly Guid NivelId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AsignaturaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid NivelAsignaturaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid EjeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid UnidadId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid Oa1Id = Guid.Parse("66666666-6666-6666-6666-666666666601");
    public static readonly Guid Oa2Id = Guid.Parse("66666666-6666-6666-6666-666666666602");
    public static readonly Guid Oa3Id = Guid.Parse("66666666-6666-6666-6666-666666666603");

    public static void Seed(AppEducativaDbContext db)
    {
        if (db.Niveles.Any(n => n.Id == NivelId))
            return;

        db.Niveles.Add(new Nivel
        {
            Id = NivelId,
            Codigo = "4B",
            Nombre = "4° básico",
            Ciclo = "Basica",
            Orden = 6
        });

        db.Asignaturas.Add(new Asignatura
        {
            Id = AsignaturaId,
            Codigo = "MAT",
            Nombre = "Matemática"
        });

        db.NivelesAsignaturas.Add(new NivelAsignatura
        {
            Id = NivelAsignaturaId,
            NivelId = NivelId,
            AsignaturaId = AsignaturaId,
            NombreEnNivel = "Matemática",
            Activa = true,
            Vigente = true,
            EstadoRevision = EstadoRevision.AprobadoParaPruebas,
            FuenteTipo = FuenteTipo,
            EsContenidoOficial = false,
            ConfianzaExtraccion = 1
        });

        db.EjesCurriculares.Add(new EjeCurricular
        {
            Id = EjeId,
            NivelAsignaturaId = NivelAsignaturaId,
            Codigo = "NUM",
            Nombre = "Números y operaciones (demo)",
            Descripcion = "Eje demostrativo — no oficial.",
            Vigente = true,
            EstadoRevision = EstadoRevision.AprobadoParaPruebas
        });

        db.Unidades.Add(new Unidad
        {
            Id = UnidadId,
            NivelAsignaturaId = NivelAsignaturaId,
            Numero = 1,
            Nombre = "Fracciones (demo)",
            Descripcion = "Unidad de demostración para pruebas locales. No es contenido oficial MINEDUC.",
            HorasPedagogicasSugeridas = 12,
            Orden = 1,
            Vigente = true,
            EstadoRevision = EstadoRevision.AprobadoParaPruebas,
            FuenteTipo = FuenteTipo,
            EsContenidoOficial = false
        });

        db.ObjetivosAprendizaje.AddRange(
            Oa(Oa1Id, 1, "DEMO OA 01", "[DEMO] Representar fracciones propias con material concreto y pictórico."),
            Oa(Oa2Id, 2, "DEMO OA 02", "[DEMO] Comparar fracciones de igual denominador en contextos cotidianos."),
            Oa(Oa3Id, 3, "DEMO OA 03", "[DEMO] Identificar fracciones equivalentes simples con apoyo visual."));

        db.UnidadObjetivos.AddRange(
            new UnidadObjetivoAprendizaje { UnidadId = UnidadId, ObjetivoAprendizajeId = Oa1Id, Orden = 1 },
            new UnidadObjetivoAprendizaje { UnidadId = UnidadId, ObjetivoAprendizajeId = Oa2Id, Orden = 2 },
            new UnidadObjetivoAprendizaje { UnidadId = UnidadId, ObjetivoAprendizajeId = Oa3Id, Orden = 3 });

        db.IndicadoresEvaluacion.AddRange(
            Ind("99999999-9999-9999-9999-999999999101", Oa1Id, 1, "Representa fracciones con material concreto (demo)."),
            Ind("99999999-9999-9999-9999-999999999102", Oa1Id, 2, "Dibuja fracciones en modelos pictóricos (demo)."),
            Ind("99999999-9999-9999-9999-999999999201", Oa2Id, 1, "Ordena fracciones de igual denominador (demo)."),
            Ind("99999999-9999-9999-9999-999999999202", Oa2Id, 2, "Explica la comparación con un ejemplo cotidiano (demo)."),
            Ind("99999999-9999-9999-9999-999999999203", Oa2Id, 3, "Identifica la fracción mayor en un par dado (demo)."),
            Ind("99999999-9999-9999-9999-999999999301", Oa3Id, 1, "Reconoce pares equivalentes simples (demo)."),
            Ind("99999999-9999-9999-9999-999999999302", Oa3Id, 2, "Justifica equivalencia con dibujo (demo)."));

        db.Habilidades.AddRange(
            new Habilidad
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777701"),
                NivelAsignaturaId = NivelAsignaturaId,
                Codigo = "H-DEMO-1",
                Descripcion = "[DEMO] Resolver problemas usando fracciones.",
                Vigente = true,
                EstadoRevision = EstadoRevision.AprobadoParaPruebas
            },
            new Habilidad
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777702"),
                NivelAsignaturaId = NivelAsignaturaId,
                Codigo = "H-DEMO-2",
                Descripcion = "[DEMO] Argumentar decisiones matemáticas con evidencia.",
                Vigente = true,
                EstadoRevision = EstadoRevision.AprobadoParaPruebas
            });

        db.Actitudes.AddRange(
            new Actitud
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888801"),
                NivelId = NivelId,
                NivelAsignaturaId = NivelAsignaturaId,
                Codigo = "A-DEMO-1",
                Descripcion = "[DEMO] Perseverar frente a desafíos matemáticos.",
                Vigente = true,
                EstadoRevision = EstadoRevision.AprobadoParaPruebas
            },
            new Actitud
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888802"),
                NivelId = NivelId,
                NivelAsignaturaId = NivelAsignaturaId,
                Codigo = "A-DEMO-2",
                Descripcion = "[DEMO] Trabajar colaborativamente en la resolución de problemas.",
                Vigente = true,
                EstadoRevision = EstadoRevision.AprobadoParaPruebas
            });
    }

    private static ObjetivoAprendizaje Oa(Guid id, int numero, string codigo, string desc) => new()
    {
        Id = id,
        NivelAsignaturaId = NivelAsignaturaId,
        EjeCurricularId = EjeId,
        Codigo = codigo,
        Numero = numero,
        Descripcion = desc,
        Tipo = TipoObjetivoAprendizaje.Basal,
        Vigente = true,
        Version = "seed-demo-1",
        EstadoRevision = EstadoRevision.AprobadoParaPruebas,
        FuenteTipo = FuenteTipo,
        EsContenidoOficial = false,
        ObservacionRevision = "SeedDemostracion — no oficial"
    };

    private static IndicadorEvaluacion Ind(string id, Guid oaId, int orden, string desc) => new()
    {
        Id = Guid.Parse(id),
        ObjetivoAprendizajeId = oaId,
        UnidadId = UnidadId,
        Descripcion = desc,
        EsSugerido = true,
        Orden = orden,
        Vigente = true,
        EstadoRevision = EstadoRevision.AprobadoParaPruebas
    };
}
