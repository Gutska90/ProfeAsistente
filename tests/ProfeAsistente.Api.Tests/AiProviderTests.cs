using ProfeAsistente.Api.Services.AI;
using ProfeAsistente.Api.Services.AI.Gemini;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProfeAsistente.Api.Configuration;

namespace ProfeAsistente.Api.Tests;

public class AiProviderTests
{
    [Fact]
    public void GeminiAiProvider_ReportsProviderName()
    {
        var client = new GeminiClient(
            new FixedHttpFactory(),
            Options.Create(new GeminiOptions
            {
                ApiKeyEnvironmentVariable = "GEMINI_API_KEY_MISSING_FOR_TEST",
                Model = "gemini-2.5-flash",
                BaseUrl = "https://generativelanguage.googleapis.com",
                EnableGeneration = false
            }),
            NullLogger<GeminiClient>.Instance);

        IAiProvider provider = new GeminiAiProvider(client);
        Assert.Equal("Gemini", provider.ProviderName);
    }

    private sealed class FixedHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
