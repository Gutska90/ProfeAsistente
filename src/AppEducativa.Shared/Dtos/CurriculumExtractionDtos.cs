using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Dtos;

public class CurriculumSourceConfig
{
    public string Nombre { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Tipo { get; set; } = "ProgramaEstudio";
    public string Formato { get; set; } = "Pdf";
    public string? Nivel { get; set; }
    public string? Asignatura { get; set; }
    public bool Activo { get; set; } = true;
}

public class DownloadedSource
{
    public string UrlOriginal { get; set; } = string.Empty;
    public string RutaArchivoLocal { get; set; } = string.Empty;
    public string HashSha256 { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public string? LastModified { get; set; }
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "application/octet-stream";
    public bool FromCache { get; set; }
}

public class CurriculumExtractionResult
{
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "ProgramaEstudio";
    public string Version { get; set; } = "1";
    public string? ExtractedText { get; set; }
    public double ConfianzaExtraccion { get; set; } = 1;
    public List<string> Advertencias { get; set; } = [];
    public List<string> Errores { get; set; } = [];

    public LevelExtractDto? Level { get; set; }
    public SubjectExtractDto? Subject { get; set; }
    public List<AxisExtractDto> Axes { get; set; } = [];
    public List<UnitExtractDto> Units { get; set; } = [];
    public List<LearningObjectiveExtractDto> LearningObjectives { get; set; } = [];
    public List<EvaluationIndicatorExtractDto> EvaluationIndicators { get; set; } = [];
    public List<SkillExtractDto> Skills { get; set; } = [];
    public List<AttitudeExtractDto> Attitudes { get; set; } = [];
    public List<OatExtractDto> TransversalObjectives { get; set; } = [];
}

public class LevelExtractDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Cycle { get; set; } = "Basica";
    public int Order { get; set; }
}

public class SubjectExtractDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class AxisExtractDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UnitExtractDto
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SuggestedHours { get; set; }
    public List<string> LearningObjectiveCodes { get; set; } = [];
}

public class LearningObjectiveExtractDto
{
    public string Code { get; set; } = string.Empty;
    public int? Number { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AxisName { get; set; }
    public string Tipo { get; set; } = "Basal";
    public bool EsObligatorio { get; set; } = true;
}

public class EvaluationIndicatorExtractDto
{
    public string LearningObjectiveCode { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool EsSugerido { get; set; } = true;
    public int Orden { get; set; }
}

public class SkillExtractDto
{
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AxisName { get; set; }
}

public class AttitudeExtractDto
{
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class OatExtractDto
{
    public string Code { get; set; } = string.Empty;
    public string? Dimension { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CurriculumValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class CurriculumDiffItem
{
    public TipoCambioCurricular Tipo { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? Observacion { get; set; }
}

public class CurriculumDiffResult
{
    public List<CurriculumDiffItem> Items { get; set; } = [];
    public int Nuevos => Items.Count(i => i.Tipo == TipoCambioCurricular.Nuevo);
    public int Modificados => Items.Count(i => i.Tipo == TipoCambioCurricular.Modificado);
    public int SinCambios => Items.Count(i => i.Tipo == TipoCambioCurricular.SinCambios);
    public int PosiblementeEliminados => Items.Count(i => i.Tipo == TipoCambioCurricular.PosiblementeEliminado);
}

public class CurriculumImportResult
{
    public Guid BatchId { get; set; }
    public bool Success { get; set; }
    public int RegistrosNuevos { get; set; }
    public int Actualizados { get; set; }
    public int SinCambios { get; set; }
    public List<string> Advertencias { get; set; } = [];
    public List<string> Errores { get; set; } = [];
}
