using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppEducativa.Shared.Offline;

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
    public List<OutboxItem> Outbox { get; set; } = [];
    public Dictionary<string, string> Cache { get; set; } = new(StringComparer.Ordinal);
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
    public static async Task<OutboxFlushResult> FlushAsync(
        IEnumerable<OutboxItem> items,
        Func<OutboxItem, CancellationToken, Task> send,
        CancellationToken cancellationToken = default)
    {
        var ordered = items.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id).ToList();
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
                error = ex.Message;
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
