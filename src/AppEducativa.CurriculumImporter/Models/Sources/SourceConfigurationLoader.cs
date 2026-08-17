using System.Text.Json;

namespace AppEducativa.CurriculumImporter.Models.Sources;

public sealed class SourceConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<CurriculumSourceDefinition>> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var sources = await JsonSerializer.DeserializeAsync<List<CurriculumSourceDefinition>>(
            stream, JsonOptions, cancellationToken) ?? [];
        foreach (var source in sources)
            Validate(source);
        return sources;
    }

    public static void Validate(CurriculumSourceDefinition source)
    {
        if (string.IsNullOrWhiteSpace(source.Id))
            throw new InvalidOperationException("La fuente curricular debe tener id.");
        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"La fuente {source.Id} debe usar una URL HTTPS absoluta.");
        if (string.IsNullOrWhiteSpace(source.DominioPermitido) ||
            !string.Equals(uri.Host, source.DominioPermitido, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"El dominio de {source.Id} no coincide con el dominio permitido.");
        if (source.IntervaloSolicitudesMs < 0)
            throw new InvalidOperationException($"El intervalo de {source.Id} no puede ser negativo.");
        if (string.Equals(source.TipoFuente, "ProgramaEstudio", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(source.NivelCodigo) || string.IsNullOrWhiteSpace(source.AsignaturaCodigo)))
            throw new InvalidOperationException($"La fuente ProgramaEstudio {source.Id} requiere nivel y asignatura.");
    }
}
