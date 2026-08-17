namespace AppEducativa.Maui.Services;

public interface IFileSaveService
{
    Task<string?> SaveAsync(byte[] bytes, string suggestedFileName, string contentType, CancellationToken ct = default);
}

/// <summary>
/// Guarda en Documents/AppEducativa. En Windows/macOS el usuario puede mover el archivo después.
/// Evita dependencias de FilePicker/FileSaver no configuradas en el proyecto.
/// </summary>
public sealed class FileSaveService : IFileSaveService
{
    public async Task<string?> SaveAsync(byte[] bytes, string suggestedFileName, string contentType, CancellationToken ct = default)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AppEducativa");
        Directory.CreateDirectory(folder);
        var dest = Path.Combine(folder, suggestedFileName);
        if (File.Exists(dest))
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            dest = Path.Combine(folder,
                Path.GetFileNameWithoutExtension(suggestedFileName) + $"_{stamp}" + Path.GetExtension(suggestedFileName));
        }

        await File.WriteAllBytesAsync(dest, bytes, ct);
        return dest;
    }
}
