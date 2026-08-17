using ProfeAsistente.Api;
using ProfeAsistente.Api.Data;

var app = ApiHostBuilder.Build(args);
await DatabaseInitializer.InitializeAsync(app.Services);
await app.RunAsync();

public partial class Program;
