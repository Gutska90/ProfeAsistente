using System.Text;
using System.Text.Json;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Offline;

namespace ProfeAsistente.Maui.Services;

public interface IOfflineSyncService
{
    int PendingCount { get; }
    string StatusText { get; }
    string? LastFlushError { get; }
    DateTimeOffset? LastSuccessfulFlushAt { get; }
    bool IsOnline { get; }
    event EventHandler? Changed;
    Task<OutboxFlushResult> FlushAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PlanificacionResumenDto>> GetPlanificacionesAsync(CancellationToken ct = default);
    Task<PlanificacionDetalleDto?> GetPlanificacionAsync(Guid id, CancellationToken ct = default);
    Task<ClaseDetalleDto?> GetClaseAsync(Guid id, CancellationToken ct = default);
    Task<ClaseDetalleDto> SaveClaseAsync(Guid id, ActualizarClaseRequest request, ClaseDetalleDto optimistic, CancellationToken ct = default);
    Task CompleteClaseAsync(Guid id, CompleteClassRequest request, CancellationToken ct = default);
    Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<CourseRosterDto?> GetClassRosterAsync(Guid classId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken ct = default);
    Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ClassDuaStrategyDto>> GetDuaAsync(Guid classId, CancellationToken ct = default);
    Task AddDuaAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LearningAssessmentDto>> GetAssessmentsAsync(Guid classId, CancellationToken ct = default);
    Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken ct = default);
    Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken ct = default);
}

public sealed class OfflineSyncService : IOfflineSyncService, IDisposable
{
    private readonly IApiClient _api;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;
    private OfflineSnapshot _snap = new();
    private bool _disposed;

    public OfflineSyncService(IApiClient api, HttpClient http)
    {
        _api = api;
        _http = http;
        _path = Path.Combine(FileSystem.AppDataDirectory, "offline-sync.json");
        Load();
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public int PendingCount => _snap.Outbox.Count;
    public string? LastFlushError => _snap.LastFlushError;
    public DateTimeOffset? LastSuccessfulFlushAt => _snap.LastSuccessfulFlushAt;
    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public string StatusText
    {
        get
        {
            var online = IsOnline ? "En línea" : "Sin red";
            if (PendingCount == 0)
            {
                var last = LastSuccessfulFlushAt is null
                    ? "sin sync reciente"
                    : $"última sync {LastSuccessfulFlushAt:HH:mm}";
                return $"{online}. Sin pendientes · {last}. La caché sirve offline.";
            }

            var err = string.IsNullOrWhiteSpace(LastFlushError)
                ? string.Empty
                : $" · Error: {LastFlushError}";
            return $"{online}. {PendingCount} pendiente(s){err}";
        }
    }

    public event EventHandler? Changed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _gate.Dispose();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);
        if (e.NetworkAccess == NetworkAccess.Internet && PendingCount > 0)
            _ = FlushAsync();
    }

    public async Task<OutboxFlushResult> FlushAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            _snap.Outbox = OutboxProcessor.Coalesce(_snap.Outbox);
            var result = await OutboxProcessor.FlushAsync(_snap.Outbox, SendAsync, ct);
            _snap.Outbox = result.Remaining.ToList();
            if (result.Sent > 0 && result.StoppedOnError is null)
            {
                _snap.LastSuccessfulFlushAt = DateTimeOffset.UtcNow;
                _snap.LastFlushError = null;
            }
            else if (result.StoppedOnError is not null)
            {
                _snap.LastFlushError = result.StoppedOnError;
                if (result.Sent > 0)
                    _snap.LastSuccessfulFlushAt = DateTimeOffset.UtcNow;
            }
            else if (result.Sent > 0)
            {
                _snap.LastSuccessfulFlushAt = DateTimeOffset.UtcNow;
                _snap.LastFlushError = null;
            }

            Save();
            return result;
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task<IReadOnlyList<PlanificacionResumenDto>> GetPlanificacionesAsync(CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Plannings, async () =>
            (IReadOnlyList<PlanificacionResumenDto>)(await _api.GetPlanificacionesAsync(ct)).ToList(), ct);

    public Task<PlanificacionDetalleDto?> GetPlanificacionAsync(Guid id, CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Planning(id), () => _api.GetPlanificacionAsync(id, ct), ct);

    public async Task<ClaseDetalleDto?> GetClaseAsync(Guid id, CancellationToken ct = default)
    {
        var clase = await ReadThroughAsync(OfflineCacheKeys.Clase(id), () => _api.GetClaseAsync(id, ct), ct);
        if (clase is not null && IsOnline)
            _ = PrefetchClassBundleAsync(id, ct);
        return clase;
    }

    public async Task<ClaseDetalleDto> SaveClaseAsync(Guid id, ActualizarClaseRequest request, ClaseDetalleDto optimistic, CancellationToken ct = default)
    {
        await SetAsync(OfflineCacheKeys.Clase(id), optimistic);
        try
        {
            var saved = await _api.ActualizarClaseAsync(id, request, ct);
            await SetAsync(OfflineCacheKeys.Clase(id), saved);
            return saved;
        }
        catch (Exception ex) when (OutboxProcessor.IsTransient(ex))
        {
            await EnqueueAsync("PUT", $"api/clases/{id}", request);
            return optimistic;
        }
    }

    public async Task CompleteClaseAsync(Guid id, CompleteClassRequest request, CancellationToken ct = default)
    {
        var cached = await GetAsync<ClaseDetalleDto>(OfflineCacheKeys.Clase(id));
        if (cached is not null)
        {
            cached.Estado = Shared.Enums.EstadoClase.Realizada;
            await SetAsync(OfflineCacheKeys.Clase(id), cached);
        }

        try
        {
            await _api.CompleteClassAsync(id, request, ct);
        }
        catch (Exception ex) when (OutboxProcessor.IsTransient(ex))
        {
            await EnqueueAsync("POST", $"api/clases/{id}/completar", request);
        }
    }

    public Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Dashboard, () => _api.GetTeacherDashboardAsync(ct), ct);

    public Task<CourseRosterDto?> GetClassRosterAsync(Guid classId, CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.ClassRoster(classId), () => _api.GetClassRosterAsync(classId, ct), ct);

    public Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Attendance(classId), async () =>
            (IReadOnlyList<AttendanceRecordDto>)(await _api.GetAttendanceAsync(classId, ct)).ToList(), ct);

    public async Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken ct = default)
    {
        var rows = request.Entries.Select(e => new AttendanceRecordDto
        {
            StudentId = e.StudentId,
            Status = e.Status,
            Justification = e.Justification
        }).ToList();
        await SetAsync(OfflineCacheKeys.Attendance(classId), (IReadOnlyList<AttendanceRecordDto>)rows);
        try
        {
            await _api.SaveAttendanceAsync(classId, request, ct);
        }
        catch (Exception ex) when (OutboxProcessor.IsTransient(ex))
        {
            await EnqueueAsync("PUT", $"api/clases/{classId}/asistencia", request);
        }
    }

    public Task<IReadOnlyList<ClassDuaStrategyDto>> GetDuaAsync(Guid classId, CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Dua(classId), async () =>
            (IReadOnlyList<ClassDuaStrategyDto>)(await _api.GetDuaStrategiesAsync(classId, ct)).ToList(), ct);

    public async Task AddDuaAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken ct = default)
    {
        try
        {
            var created = await _api.AddDuaStrategyAsync(classId, request, ct);
            var list = (await GetAsync<List<ClassDuaStrategyDto>>(OfflineCacheKeys.Dua(classId))) ?? [];
            list.Add(created);
            await SetAsync(OfflineCacheKeys.Dua(classId), list);
        }
        catch (Exception ex) when (OutboxProcessor.IsTransient(ex))
        {
            var list = (await GetAsync<List<ClassDuaStrategyDto>>(OfflineCacheKeys.Dua(classId))) ?? [];
            list.Add(new ClassDuaStrategyDto { Id = Guid.NewGuid(), Principle = request.Principle, Strategy = request.Strategy });
            await SetAsync(OfflineCacheKeys.Dua(classId), list);
            await EnqueueAsync("POST", $"api/clases/{classId}/dua", request);
        }
    }

    public Task<IReadOnlyList<LearningAssessmentDto>> GetAssessmentsAsync(Guid classId, CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Assessments(classId), async () =>
            (IReadOnlyList<LearningAssessmentDto>)(await _api.GetAssessmentsAsync(classId: classId, ct: ct)).ToList(), ct);

    public Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken ct = default)
        => ReadThroughAsync(OfflineCacheKeys.Scores(assessmentId), async () =>
            (IReadOnlyList<AssessmentScoreDto>)(await _api.GetAssessmentScoresAsync(assessmentId, ct)).ToList(), ct);

    public async Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken ct = default)
    {
        var previous = (await GetAsync<List<AssessmentScoreDto>>(OfflineCacheKeys.Scores(assessmentId))) ?? [];
        var names = previous.ToDictionary(s => s.StudentId, s => s.StudentName);
        var cached = scores.Select(s => new AssessmentScoreDto
        {
            StudentId = s.StudentId,
            StudentName = names.TryGetValue(s.StudentId, out var n) ? n : string.Empty,
            Score = s.Score,
            AchievementLevel = s.AchievementLevel,
            Feedback = s.Feedback
        }).ToList();
        await SetAsync(OfflineCacheKeys.Scores(assessmentId), cached);

        try
        {
            await _api.SaveAssessmentScoresAsync(assessmentId, scores, ct);
        }
        catch (Exception ex) when (OutboxProcessor.IsTransient(ex))
        {
            await EnqueueAsync("PUT", $"api/evaluaciones/{assessmentId}/puntajes", scores);
        }
    }

    private async Task PrefetchClassBundleAsync(Guid classId, CancellationToken ct)
    {
        try
        {
            await GetClassRosterAsync(classId, ct);
            await GetAttendanceAsync(classId, ct);
            await GetDuaAsync(classId, ct);
            var assessments = await GetAssessmentsAsync(classId, ct);
            foreach (var a in assessments.Take(3))
                await GetScoresAsync(a.Id, ct);
        }
        catch
        {
            // Prefetch best-effort: no tumbar la ficha de clase.
        }
    }

    private async Task<T> ReadThroughAsync<T>(string key, Func<Task<T>> fetch, CancellationToken ct)
    {
        try
        {
            var value = await fetch();
            if (value is not null)
                await SetAsync(key, value);
            _ = FlushAsync(ct);
            return value;
        }
        catch (Exception ex) when (OutboxProcessor.IsTransient(ex))
        {
            var cached = await GetAsync<T>(key);
            if (cached is not null) return cached;
            throw new InvalidOperationException("Sin conexión y no hay copia local de estos datos. Ábralos una vez con la API encendida.");
        }
    }

    private async Task EnqueueAsync(string method, string path, object body)
    {
        await _gate.WaitAsync();
        try
        {
            var item = new OutboxItem
            {
                Method = method,
                Path = path,
                JsonBody = JsonSerializer.Serialize(body, OfflineJson.Options)
            };
            _snap.Outbox.Add(item);
            _snap.Outbox = OutboxProcessor.Coalesce(_snap.Outbox);
            Save();
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task SendAsync(OutboxItem item, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(new HttpMethod(item.Method), item.Path);
        if (!string.IsNullOrWhiteSpace(item.JsonBody))
            req.Content = new StringContent(item.JsonBody, Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API {(int)response.StatusCode}: {text}");
        }
    }

    private async Task SetAsync<T>(string key, T value)
    {
        await _gate.WaitAsync();
        try
        {
            _snap.Cache[key] = JsonSerializer.Serialize(value, OfflineJson.Options);
            Save();
        }
        finally { _gate.Release(); }
    }

    private async Task<T?> GetAsync<T>(string key)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_snap.Cache.TryGetValue(key, out var json)) return default;
            return JsonSerializer.Deserialize<T>(json, OfflineJson.Options);
        }
        finally { _gate.Release(); }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var json = File.ReadAllText(_path);
            _snap = JsonSerializer.Deserialize<OfflineSnapshot>(json, OfflineJson.Options) ?? new();
            _snap.Outbox = OutboxProcessor.Coalesce(_snap.Outbox);
        }
        catch { _snap = new(); }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var tmp = _path + ".tmp";
        var json = JsonSerializer.Serialize(_snap, OfflineJson.Options);
        File.WriteAllText(tmp, json);
        File.Move(tmp, _path, overwrite: true);
    }
}
