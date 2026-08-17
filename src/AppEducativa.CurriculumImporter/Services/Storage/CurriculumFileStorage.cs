namespace AppEducativa.CurriculumImporter.Services.Storage;

public interface ICurriculumFileStorage
{
    string DownloadsDirectory { get; }
    string ExtractedDirectory { get; }
    string ImportsDirectory { get; }
    Task<string> SaveTextAsync(string fileName, string content, CancellationToken cancellationToken = default);
}

public sealed class CurriculumFileStorage : ICurriculumFileStorage
{
    public CurriculumFileStorage(string? root = null)
    {
        var baseRoot = root ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "Curriculum");
        DownloadsDirectory = Path.Combine(baseRoot, "Downloads");
        ExtractedDirectory = Path.Combine(baseRoot, "Extracted");
        ImportsDirectory = Path.Combine(baseRoot, "Imports");
        Directory.CreateDirectory(DownloadsDirectory);
        Directory.CreateDirectory(ExtractedDirectory);
        Directory.CreateDirectory(ImportsDirectory);
    }

    public string DownloadsDirectory { get; }
    public string ExtractedDirectory { get; }
    public string ImportsDirectory { get; }

    public async Task<string> SaveTextAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(ExtractedDirectory, Path.GetFileName(fileName));
        await File.WriteAllTextAsync(path, content, cancellationToken);
        return path;
    }
}
