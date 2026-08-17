using System.Security.Cryptography;
using System.Text;
using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Data;

/// <summary>
/// Catálogo de niveles/asignaturas/unidades del sistema escolar chileno para planificar.
/// No es el texto oficial MINEDUC; las OA de catálogo son plantillas.
/// Idempotente: no duplica códigos ni pisa unidades ya importadas.
/// </summary>
public static class ChileanCurriculumCatalogSeed
{
    public const string FuenteTipo = "SeedCatalogo";

    public static void Ensure(ProfeAsistenteDbContext db)
    {
        var niveles = EnsureNiveles(db);
        var asignaturas = EnsureAsignaturas(db);
        var existingNa = TrackedAndStored(db, db.NivelesAsignaturas)
            .GroupBy(x => (x.NivelId, x.AsignaturaId))
            .Select(g => g.First())
            .ToList();
        var naWithUnits = TrackedAndStored(db, db.Unidades)
            .Select(u => u.NivelAsignaturaId)
            .ToHashSet();

        foreach (var (levelCode, subjectCode, displayName) in Offerings())
        {
            if (!niveles.TryGetValue(levelCode, out var nivel) || !asignaturas.TryGetValue(subjectCode, out var asignatura))
                continue;

            var na = existingNa.FirstOrDefault(x => x.NivelId == nivel.Id && x.AsignaturaId == asignatura.Id);
            if (na is null)
            {
                na = new NivelAsignatura
                {
                    Id = StableId($"na:{nivel.Codigo}:{asignatura.Codigo}"),
                    NivelId = nivel.Id,
                    AsignaturaId = asignatura.Id,
                    NombreEnNivel = displayName,
                    Activa = true,
                    Vigente = true,
                    EstadoRevision = EstadoRevision.AprobadoParaPruebas,
                    FuenteTipo = FuenteTipo,
                    EsContenidoOficial = false
                };
                db.NivelesAsignaturas.Add(na);
                existingNa.Add(na);
            }
            else
            {
                na.Activa = true;
                na.Vigente = true;
                if (na.EstadoRevision is EstadoRevision.Pendiente or EstadoRevision.Rechazado)
                    na.EstadoRevision = EstadoRevision.AprobadoParaPruebas;
                if (string.IsNullOrWhiteSpace(na.NombreEnNivel))
                    na.NombreEnNivel = displayName;
            }

            if (naWithUnits.Contains(na.Id))
                continue;
            AddTemplateUnits(db, na, nivel, asignatura, displayName);
            naWithUnits.Add(na.Id);
        }
    }

    private static Dictionary<string, Nivel> EnsureNiveles(ProfeAsistenteDbContext db)
    {
        var existing = TrackedAndStored(db, db.Niveles)
            .GroupBy(n => n.Codigo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var (orden, codigo, nombre, ciclo) in Niveles())
        {
            if (existing.ContainsKey(codigo)) continue;
            var nivel = new Nivel
            {
                Id = StableId("nivel:" + codigo),
                Codigo = codigo,
                Nombre = nombre,
                Ciclo = ciclo,
                Orden = orden
            };
            db.Niveles.Add(nivel);
            existing[codigo] = nivel;
        }
        return existing;
    }

    private static Dictionary<string, Asignatura> EnsureAsignaturas(ProfeAsistenteDbContext db)
    {
        var existing = TrackedAndStored(db, db.Asignaturas)
            .GroupBy(a => a.Codigo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var (codigo, nombre) in Asignaturas())
        {
            if (existing.ContainsKey(codigo)) continue;
            var a = new Asignatura { Id = StableId("asig:" + codigo), Codigo = codigo, Nombre = nombre };
            db.Asignaturas.Add(a);
            existing[codigo] = a;
        }
        return existing;
    }

    private static void AddTemplateUnits(
        ProfeAsistenteDbContext db, NivelAsignatura na, Nivel nivel, Asignatura asignatura, string displayName)
    {
        var units = UnitsFor(asignatura.Codigo);
        for (var i = 0; i < units.Length; i++)
        {
            var numero = i + 1;
            var unidad = new Unidad
            {
                Id = StableId($"u:{nivel.Codigo}:{asignatura.Codigo}:{numero}"),
                NivelAsignaturaId = na.Id,
                Numero = numero,
                Nombre = units[i],
                Descripcion = "Estructura para planificar. No reemplaza el programa oficial MINEDUC; puede importarlo en Administrar currículum.",
                Orden = numero,
                HorasPedagogicasSugeridas = 12,
                Vigente = true,
                EstadoRevision = EstadoRevision.AprobadoParaPruebas,
                PublicationStatus = CurriculumPublicationStatus.Published,
                FuenteTipo = FuenteTipo,
                EsContenidoOficial = false
            };
            db.Unidades.Add(unidad);

            var oa = new ObjetivoAprendizaje
            {
                Id = StableId($"oa:{nivel.Codigo}:{asignatura.Codigo}:{numero}"),
                NivelAsignaturaId = na.Id,
                Codigo = $"{asignatura.Codigo}-{nivel.Codigo}-U{numero}-OA1",
                Numero = numero,
                Descripcion = $"[{nivel.Nombre} · {displayName} · Unidad {numero}] Planifique clases de «{units[i]}». Importe el programa oficial para usar OA autorizados.",
                Tipo = TipoObjetivoAprendizaje.Basal,
                Vigente = true,
                Version = "catalogo-1",
                EstadoRevision = EstadoRevision.AprobadoParaPruebas,
                PublicationStatus = CurriculumPublicationStatus.Published,
                FuenteTipo = FuenteTipo,
                EsContenidoOficial = false,
                ObservacionRevision = "SeedCatalogo — plantilla, no oficial"
            };
            db.ObjetivosAprendizaje.Add(oa);
            db.UnidadObjetivos.Add(new UnidadObjetivoAprendizaje
            {
                UnidadId = unidad.Id,
                ObjetivoAprendizajeId = oa.Id,
                Orden = 1
            });
        }
    }

    private static IEnumerable<(int Orden, string Codigo, string Nombre, string Ciclo)> Niveles()
    {
        yield return (1, "NT1", "Pre-kínder (NT1)", "Parvularia");
        yield return (2, "NT2", "Kínder (NT2)", "Parvularia");
        for (var i = 1; i <= 8; i++)
            yield return (2 + i, $"{i}B", $"{i}° básico", "Basica");
        for (var i = 1; i <= 4; i++)
            yield return (10 + i, $"{i}M", $"{i}° medio", "Media");
    }

    private static IEnumerable<(string Codigo, string Nombre)> Asignaturas()
    {
        yield return ("PVB", "Lenguaje verbal");
        yield return ("PMT", "Pensamiento matemático");
        yield return ("PEN", "Exploración del entorno natural");
        yield return ("PSC", "Exploración del entorno social");
        yield return ("PCO", "Corporalidad y movimiento");
        yield return ("PID", "Identidad y autonomía");
        yield return ("PCV", "Convivencia y ciudadanía");
        yield return ("PAR", "Lenguajes artísticos");
        yield return ("LEN", "Lenguaje y Comunicación");
        yield return ("MAT", "Matemática");
        yield return ("HIS", "Historia, Geografía y Ciencias Sociales");
        yield return ("CN", "Ciencias Naturales");
        yield return ("ING", "Inglés");
        yield return ("ART", "Artes Visuales");
        yield return ("MUS", "Música");
        yield return ("EDF", "Educación Física y Salud");
        yield return ("TEC", "Tecnología");
        yield return ("ORI", "Orientación");
        yield return ("REL", "Religión");
        yield return ("BIO", "Biología");
        yield return ("FIS", "Física");
        yield return ("QUI", "Química");
        yield return ("EDC", "Educación Ciudadana");
        yield return ("FIL", "Filosofía");
        yield return ("CPC", "Ciencias para la Ciudadanía");
    }

    private static IEnumerable<(string LevelCode, string SubjectCode, string DisplayName)> Offerings()
    {
        string[] parvularia = ["PVB", "PMT", "PEN", "PSC", "PCO", "PID", "PCV", "PAR"];
        foreach (var lv in new[] { "NT1", "NT2" })
        foreach (var s in parvularia)
            yield return (lv, s, NameOf(s));

        string[] basica = ["LEN", "MAT", "HIS", "CN", "ING", "ART", "MUS", "EDF", "TEC", "ORI", "REL"];
        for (var i = 1; i <= 6; i++)
        foreach (var s in basica)
            yield return ($"{i}B", s, s == "LEN" ? "Lenguaje y Comunicación" : NameOf(s));

        string[] septOct = ["LEN", "MAT", "HIS", "CN", "ING", "ART", "MUS", "EDF", "TEC", "ORI", "REL"];
        foreach (var lv in new[] { "7B", "8B" })
        foreach (var s in septOct)
            yield return (lv, s, s == "LEN" ? "Lengua y Literatura" : NameOf(s));

        string[] mediaBaja = ["LEN", "MAT", "HIS", "BIO", "FIS", "QUI", "ING", "ART", "MUS", "EDF", "TEC", "ORI", "REL"];
        foreach (var lv in new[] { "1M", "2M" })
        foreach (var s in mediaBaja)
            yield return (lv, s, s == "LEN" ? "Lengua y Literatura" : NameOf(s));

        string[] mediaAlta = ["LEN", "MAT", "EDC", "FIL", "CPC", "BIO", "FIS", "QUI", "ING", "ART", "MUS", "EDF", "TEC", "ORI", "REL"];
        foreach (var lv in new[] { "3M", "4M" })
        foreach (var s in mediaAlta)
            yield return (lv, s, s == "LEN" ? "Lengua y Literatura" : NameOf(s));
    }

    private static string NameOf(string codigo) => Asignaturas().First(a => a.Codigo == codigo).Nombre;

    private static string[] UnitsFor(string subjectCode) => subjectCode switch
    {
        "MAT" or "PMT" => ["Números", "Geometría y medición", "Datos y azar", "Patrones y álgebra"],
        "LEN" or "PVB" => ["Comunicación oral", "Lectura", "Escritura", "Investigación"],
        "HIS" or "PSC" or "EDC" => ["Territorio y sociedad", "Historia", "Formación ciudadana", "Economía y geografía"],
        "CN" or "PEN" or "CPC" => ["Seres vivos", "Materia y energía", "Tierra y universo", "Investigación científica"],
        "BIO" => ["Organismo", "Célula y genética", "Ecología", "Salud"],
        "FIS" => ["Movimiento", "Energía", "Ondas", "Electricidad"],
        "QUI" => ["Materia", "Reacciones", "Química del carbono", "Química y sociedad"],
        "ING" => ["Comprensión oral", "Comprensión lectora", "Expresión escrita", "Interacción oral"],
        "ART" or "PAR" => ["Observar y apreciar", "Crear", "Reflexionar", "Exponer"],
        "MUS" => ["Escuchar", "Interpretar", "Crear", "Contextualizar"],
        "EDF" or "PCO" => ["Vida activa", "Deportes y juegos", "Vida al aire libre", "Salud"],
        "TEC" => ["Diseñar", "Hacer", "Evaluar", "Ciudadanía digital"],
        "ORI" or "PID" or "PCV" => ["Identidad", "Convivencia", "Vocación", "Bienestar"],
        "FIL" => ["Problemas filosóficos", "Conocimiento", "Ética", "Sociedad"],
        "REL" => ["Comunidad", "Textos", "Ética", "Celebraciones"],
        _ => ["Unidad 1", "Unidad 2", "Unidad 3", "Unidad 4"]
    };

    private static IEnumerable<T> TrackedAndStored<T>(ProfeAsistenteDbContext db, DbSet<T> set) where T : class
        => db.ChangeTracker.Entries<T>().Select(e => e.Entity).Concat(set.AsNoTracking());

    internal static Guid StableId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("ProfeAsistente.Catalog:" + key));
        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
