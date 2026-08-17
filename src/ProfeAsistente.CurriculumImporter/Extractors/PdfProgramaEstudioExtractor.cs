using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.Shared.Dtos;
using UglyToad.PdfPig;

namespace ProfeAsistente.CurriculumImporter.Extractors;

/// <summary>
/// Extrae texto de PDF. No inventa OA: si no puede estructurar, deja advertencias y confianza baja.
/// </summary>
public class PdfProgramaEstudioExtractor : ICurriculumExtractor
{
    public bool CanHandle(CurriculumSourceConfig source) =>
        source.Formato.Equals("Pdf", StringComparison.OrdinalIgnoreCase) &&
        (source.Tipo.Equals("ProgramaEstudio", StringComparison.OrdinalIgnoreCase) ||
         source.Tipo.Equals("BaseCurricular", StringComparison.OrdinalIgnoreCase));

    public Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceConfig source,
        DownloadedSource downloadedSource,
        CancellationToken cancellationToken = default)
    {
        var result = new CurriculumExtractionResult
        {
            SourceTitle = source.Nombre,
            SourceUrl = source.Url,
            DocumentType = source.Tipo,
            Version = DateTime.UtcNow.ToString("yyyy"),
            ConfianzaExtraccion = 0.2
        };

        try
        {
            using var stream = new MemoryStream(downloadedSource.Content);
            using var doc = PdfDocument.Open(stream);
            var sb = new System.Text.StringBuilder();
            foreach (var page in doc.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                sb.AppendLine(page.Text);
            }

            var text = sb.ToString();
            result.ExtractedText = text.Length > 200_000 ? text[..200_000] : text;

            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 80)
            {
                result.Errores.Add("El PDF no contiene texto extraíble (posible escaneo). Requiere revisión manual o import JSON.");
                result.ConfianzaExtraccion = 0;
                return Task.FromResult(result);
            }

            result.Advertencias.Add(
                "Extracción PDF: se obtuvo texto, pero no se estructuraron OA automáticamente. " +
                "Use importación JSON validada o complete la revisión manual. No se inventaron códigos.");
            result.ConfianzaExtraccion = 0.35;

            if (!string.IsNullOrWhiteSpace(source.Nivel) && !string.IsNullOrWhiteSpace(source.Asignatura))
            {
                result.Level = new LevelExtractDto
                {
                    Code = "PEND",
                    Name = source.Nivel,
                    Cycle = "Basica",
                    Order = 0
                };
                result.Subject = new SubjectExtractDto
                {
                    Code = "PEND",
                    Name = source.Asignatura
                };
                result.Advertencias.Add("Nivel/asignatura tomados de la configuración de fuente; códigos pendientes de validación.");
            }
        }
        catch (Exception ex)
        {
            result.Errores.Add($"Error al leer PDF: {ex.Message}");
            result.ConfianzaExtraccion = 0;
        }

        return Task.FromResult(result);
    }
}
