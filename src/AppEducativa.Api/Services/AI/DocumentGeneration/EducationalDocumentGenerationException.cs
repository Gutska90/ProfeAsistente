namespace AppEducativa.Api.Services.AI.DocumentGeneration;

public sealed class EducationalDocumentGenerationException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public EducationalDocumentGenerationException(string message, string errorCode, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
