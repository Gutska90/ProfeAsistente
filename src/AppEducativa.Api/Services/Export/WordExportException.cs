namespace AppEducativa.Api.Services.Export;

public sealed class WordExportException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public WordExportException(string message, string errorCode, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
