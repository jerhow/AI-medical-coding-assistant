using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MedicalCodingAssistant.Services;
using MedicalCodingAssistant.Services.Interfaces;
using Serilog;

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

logger.Information("Serilog is configured and ready.");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog(logger);
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IICD10SearchService, ICD10SearchService>();
        services.AddSingleton<IOpenAIService, OpenAIService>();
        services.AddSingleton<GptLoggingService>();
    })
    .Build();

host.Run();
