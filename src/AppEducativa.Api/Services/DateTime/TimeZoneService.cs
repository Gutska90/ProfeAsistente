namespace AppEducativa.Api.Services.DateTimeServices;

public interface ITimeZoneService
{
    bool TryResolve(string timeZoneId, out TimeZoneInfo timeZone);
    string NormalizeId(string timeZoneId);
}

public sealed class TimeZoneService : ITimeZoneService
{
    private static readonly Dictionary<string, string> WindowsToIana = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pacific SA Standard Time"] = "America/Santiago",
        ["Chile Standard Time"] = "America/Santiago"
    };

    private static readonly Dictionary<string, string> IanaToWindows = new(StringComparer.OrdinalIgnoreCase)
    {
        ["America/Santiago"] = "Pacific SA Standard Time"
    };

    public string NormalizeId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return "America/Santiago";
        var id = timeZoneId.Trim();
        if (WindowsToIana.TryGetValue(id, out var iana))
            return iana;
        return id;
    }

    public bool TryResolve(string timeZoneId, out TimeZoneInfo timeZone)
    {
        var normalized = NormalizeId(timeZoneId);
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(normalized);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            if (IanaToWindows.TryGetValue(normalized, out var windowsId))
            {
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                    return true;
                }
                catch (TimeZoneNotFoundException)
                {
                    // fall through
                }
            }
        }
        catch (InvalidTimeZoneException)
        {
            // fall through
        }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }
}
