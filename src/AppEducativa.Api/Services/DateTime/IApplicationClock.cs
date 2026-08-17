namespace AppEducativa.Api.Services.DateTimeServices;

public interface IApplicationClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemApplicationClock : IApplicationClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
