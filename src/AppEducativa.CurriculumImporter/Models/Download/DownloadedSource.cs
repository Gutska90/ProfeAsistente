namespace AppEducativa.CurriculumImporter.Models.Download;

public sealed class DownloadedSource
{
    public string SourceId { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public string LocalFilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string? ETag { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public DateTimeOffset DownloadedAt { get; init; }
    public bool WasNotModified { get; init; }
    public byte[]? Content { get; init; }
}
