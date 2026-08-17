namespace AppEducativa.Api.Services.AI.ClassGeneration;

public class ClassGenerationException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public ClassGenerationException(string message, string errorCode, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
