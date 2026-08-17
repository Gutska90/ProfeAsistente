using AppEducativa.CurriculumImporter.Models.Download;
using AppEducativa.CurriculumImporter.Models.Sources;

namespace AppEducativa.CurriculumImporter.Services.Download;

public interface ISourceDownloader
{
    Task<DownloadedSource> DownloadAsync(CurriculumSourceDefinition source, CancellationToken cancellationToken = default);
}

public sealed class SourceDownloadException : Exception
{
    public SourceDownloadException(string message, Exception? innerException = null) : base(message, innerException) { }
}
