using ProfeAsistente.Shared.Offline;

namespace ProfeAsistente.Api.Tests;

public class OutboxProcessorTests
{
    [Fact]
    public async Task Flush_EnviaEnOrdenYSeDetieneEnElPrimeroQueFalla()
    {
        var a = new OutboxItem { Path = "a", CreatedAt = DateTimeOffset.UtcNow };
        var b = new OutboxItem { Path = "b", CreatedAt = DateTimeOffset.UtcNow.AddSeconds(1) };
        var c = new OutboxItem { Path = "c", CreatedAt = DateTimeOffset.UtcNow.AddSeconds(2) };
        var sent = new List<string>();

        var result = await OutboxProcessor.FlushAsync([a, b, c], (item, _) =>
        {
            sent.Add(item.Path);
            if (item.Path == "b")
                throw new HttpRequestException("sin red");
            return Task.CompletedTask;
        });

        Assert.Equal(["a", "b"], sent);
        Assert.Equal(1, result.Sent);
        Assert.Equal(2, result.Remaining.Count);
        Assert.Equal("b", result.Remaining[0].Path);
        Assert.Equal(1, result.Remaining[0].Attempts);
        Assert.Equal("c", result.Remaining[1].Path);
    }

    [Fact]
    public async Task Flush_VaciaLaColaSiTodoSaleBien()
    {
        var items = new[]
        {
            new OutboxItem { Path = "1" },
            new OutboxItem { Path = "2" }
        };
        var result = await OutboxProcessor.FlushAsync(items, (_, _) => Task.CompletedTask);
        Assert.Equal(2, result.Sent);
        Assert.Empty(result.Remaining);
        Assert.Null(result.StoppedOnError);
    }

    [Fact]
    public void IsTransient_DetectaFallasDeRed()
    {
        Assert.True(OutboxProcessor.IsTransient(new HttpRequestException("offline")));
        Assert.True(OutboxProcessor.IsTransient(new TaskCanceledException()));
        Assert.False(OutboxProcessor.IsTransient(new InvalidOperationException("negocio")));
    }
}
