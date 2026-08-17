namespace AppEducativa.Api.Services.Curriculum;

public sealed class CurriculumReviewException : Exception
{
    public int StatusCode { get; }

    public CurriculumReviewException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
