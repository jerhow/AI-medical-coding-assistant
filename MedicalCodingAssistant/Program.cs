// using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MedicalCodingAssistant.Services;
using MedicalCodingAssistant.Services.Interfaces;
using Serilog;
using Serilog.Formatting.Json;

var basePath = AppContext.BaseDirectory;
var projectRoot = Directory.GetParent(basePath)?.Parent?.Parent?.Parent?.FullName
    ?? throw new Exception("Could not determine project root directory.");

var logsPath = Path.Combine(projectRoot, "Logs");
Directory.CreateDirectory(logsPath); // Ensure the Logs directory exists

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console() // readable output during development
    .WriteTo.File(
        formatter: new JsonFormatter(),
        path: Path.Combine(logsPath, "gpt-log-.jsonl"),
        rollingInterval: RollingInterval.Day,
        shared: true
    )
    .CreateLogger();

logger.Information("Serilog is configured and ready to log to the filesystem.");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();    // Remove default logging (console only)
        logging.AddSerilog(logger);  // Plug in Serilog
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IICD10SearchService, ICD10SearchService>();
        services.AddSingleton<IOpenAIService, OpenAIService>();
        services.AddSingleton<GptLoggingService>();
    })
    .Build();

host.Run();
