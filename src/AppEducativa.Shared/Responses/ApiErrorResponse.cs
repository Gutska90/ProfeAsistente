namespace AppEducativa.Shared.Responses;

public class ApiErrorResponse
{
    public int Status { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
}
