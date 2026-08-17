using AppEducativa.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Exports;

[QueryProperty(nameof(ExportIdText), "exportId")]
public partial class ExportProgressViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IFileSaveService _files;

    public ExportProgressViewModel(IApiClient api, IFileSaveService files)
    {
        _api = api;
        _files = files;
    }

    [ObservableProperty] private string exportIdText = string.Empty;
    [ObservableProperty] private string statusText = "Validando archivo…";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? savedPath;

    partial void OnExportIdTextChanged(string value) => _ = DescargarYGuardarAsync();

    [RelayCommand]
    public async Task DescargarYGuardarAsync()
    {
        if (!Guid.TryParse(ExportIdText, out var id)) return;
        try
        {
            IsBusy = true;
            StatusText = "Descargando…";
            var meta = await _api.GetExportAsync(id);
            if (meta is null)
            {
                MensajeEstado = "Exportación no encontrada.";
                return;
            }

            if (!string.Equals(meta.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                MensajeEstado = meta.ErrorMessage ?? $"Estado: {meta.Status}";
                return;
            }

            var (bytes, fileName) = await _api.DownloadExportAsync(id);
            StatusText = "Guardando…";
            SavedPath = await _files.SaveAsync(bytes, fileName, meta.ContentType);
            StatusText = "Completado";
            MensajeEstado = SavedPath is null
                ? "No se pudo guardar el archivo."
                : $"El archivo se guardó correctamente:\n{SavedPath}";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo generar/descargar el documento: {ex.Message}";
            StatusText = "Error";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
