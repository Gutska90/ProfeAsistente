using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProfeAsistente.Shared.Offline;

public static class OfflineJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class OutboxItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Method { get; set; } = "PUT";
    public string Path { get; set; } = string.Empty;
    public string? JsonBody { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

public sealed class OfflineSnapshot
{
    public int Version { get; set; } = 1;
    public List<OutboxItem> Outbox { get; set; } = [];
    public Dictionary<string, string> Cache { get; set; } = new(StringComparer.Ordinal);
    public DateTimeOffset? LastSuccessfulFlushAt { get; set; }
    public string? LastFlushError { get; set; }
}

public sealed class OutboxFlushResult
{
    public int Sent { get; init; }
    public IReadOnlyList<OutboxItem> Remaining { get; init; } = [];
    public string? StoppedOnError { get; init; }
}

public static class OfflineCacheKeys
{
    public const string Plannings = "plannings";
    public const string Dashboard = "dashboard";
    public static string Planning(Guid id) => $"planning:{id:N}";
    public static string Clase(Guid id) => $"clase:{id:N}";
    public static string Attendance(Guid classId) => $"attendance:{classId:N}";
    public static string ClassRoster(Guid classId) => $"roster:{classId:N}";
    public static string Dua(Guid classId) => $"dua:{classId:N}";
    public static string Assessments(Guid classId) => $"assessments:{classId:N}";
    public static string Scores(Guid assessmentId) => $"scores:{assessmentId:N}";
}

public static class OutboxProcessor
{
    /// <summary>Tras este número de intentos fallidos el ítem se reporta como bloqueante (sigue en cola).</summary>
    public const int MaxAttemptsBeforeAlert = 5;

    /// <summary>
    /// Para PUT al mismo path, conserva solo el cuerpo más reciente (evita reenviar borradores viejos).
    /// POST/DELETE no se fusionan.
    /// </summary>
    public static List<OutboxItem> Coalesce(IEnumerable<OutboxItem> items)
    {
        var ordered = items.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id).ToList();
        var result = new List<OutboxItem>();
        var putIndexByPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in ordered)
        {
            if (string.Equals(item.Method, "PUT", StringComparison.OrdinalIgnoreCase))
            {
                if (putIndexByPath.TryGetValue(item.Path, out var idx))
                {
                    var previous = result[idx];
                    item.Attempts = Math.Max(item.Attempts, previous.Attempts);
                    result[idx] = item;
                }
                else
                {
                    putIndexByPath[item.Path] = result.Count;
                    result.Add(item);
                }
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }

    public static async Task<OutboxFlushResult> FlushAsync(
        IEnumerable<OutboxItem> items,
        Func<OutboxItem, CancellationToken, Task> send,
        CancellationToken cancellationToken = default)
    {
        var ordered = Coalesce(items);
        var remaining = new List<OutboxItem>();
        var sent = 0;
        string? error = null;

        for (var i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            try
            {
                await send(item, cancellationToken);
                sent++;
            }
            catch (Exception ex)
            {
                item.Attempts++;
                item.LastError = ex.Message;
                error = item.Attempts >= MaxAttemptsBeforeAlert
                    ? $"Tras {item.Attempts} intentos: {ex.Message}"
                    : ex.Message;
                remaining.Add(item);
                remaining.AddRange(ordered.Skip(i + 1));
                break;
            }
        }

        return new OutboxFlushResult { Sent = sent, Remaining = remaining, StoppedOnError = error };
    }

    public static bool IsTransient(Exception ex)
        => ex is HttpRequestException or TaskCanceledException or TimeoutException or IOException;
}
