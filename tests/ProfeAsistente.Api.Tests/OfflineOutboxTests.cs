using ProfeAsistente.Shared.Offline;

namespace ProfeAsistente.Api.Tests;

public class OfflineOutboxTests
{
    [Fact]
    public void Coalesce_KeepsLatestPut_ForSamePath()
    {
        var path = "api/clases/abc";
        var items = new[]
        {
            new OutboxItem { Method = "PUT", Path = path, JsonBody = "v1", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) },
            new OutboxItem { Method = "PUT", Path = path, JsonBody = "v2", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new OutboxItem { Method = "POST", Path = path + "/completar", JsonBody = "{}", CreatedAt = DateTimeOffset.UtcNow }
        };

        var coalesced = OutboxProcessor.Coalesce(items);

        Assert.Equal(2, coalesced.Count);
        Assert.Equal("v2", coalesced[0].JsonBody);
        Assert.Equal("POST", coalesced[1].Method);
    }

    [Fact]
    public void Coalesce_DoesNotMergeDistinctPutPaths()
    {
        var items = new[]
        {
            new OutboxItem { Method = "PUT", Path = "api/a", JsonBody = "1" },
            new OutboxItem { Method = "PUT", Path = "api/b", JsonBody = "2" }
        };

        Assert.Equal(2, OutboxProcessor.Coalesce(items).Count);
    }

    [Fact]
    public async Task Flush_StopsOnFirstFailure_AndKeepsOrder()
    {
        var calls = new List<string>();
        var items = new[]
        {
            new OutboxItem { Method = "PUT", Path = "ok1", CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-3) },
            new OutboxItem { Method = "PUT", Path = "fail", CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-2) },
            new OutboxItem { Method = "PUT", Path = "ok2", CreatedAt = DateTimeOffset.UtcNow.AddSeconds(-1) }
        };

        var result = await OutboxProcessor.FlushAsync(items, (item, _) =>
        {
            calls.Add(item.Path);
            if (item.Path == "fail")
                throw new HttpRequestException("boom");
            return Task.CompletedTask;
        });

        Assert.Equal(1, result.Sent);
        Assert.Equal(2, result.Remaining.Count);
        Assert.Equal("fail", result.Remaining[0].Path);
        Assert.Equal("ok2", result.Remaining[1].Path);
        Assert.Equal(1, result.Remaining[0].Attempts);
        Assert.Contains("boom", result.StoppedOnError);
        Assert.Equal(new[] { "ok1", "fail" }, calls);
    }

    [Fact]
    public async Task Flush_CoalescesBeforeSending()
    {
        var sentBodies = new List<string?>();
        var items = new[]
        {
            new OutboxItem { Method = "PUT", Path = "api/x", JsonBody = "old", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new OutboxItem { Method = "PUT", Path = "api/x", JsonBody = "new", CreatedAt = DateTimeOffset.UtcNow }
        };

        var result = await OutboxProcessor.FlushAsync(items, (item, _) =>
        {
            sentBodies.Add(item.JsonBody);
            return Task.CompletedTask;
        });

        Assert.Equal(1, result.Sent);
        Assert.Empty(result.Remaining);
        Assert.Equal(new[] { "new" }, sentBodies);
    }

    [Fact]
    public void IsTransient_RecognizesNetworkFailures()
    {
        Assert.True(OutboxProcessor.IsTransient(new HttpRequestException()));
        Assert.True(OutboxProcessor.IsTransient(new TaskCanceledException()));
        Assert.True(OutboxProcessor.IsTransient(new TimeoutException()));
        Assert.True(OutboxProcessor.IsTransient(new IOException()));
        Assert.False(OutboxProcessor.IsTransient(new InvalidOperationException()));
    }
}
