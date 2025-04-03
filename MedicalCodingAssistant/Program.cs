using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MedicalCodingAssistant.Services;
using MedicalCodingAssistant.Services.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IICD10SearchService, ICD10SearchService>();
        services.AddSingleton<IOpenAIService, OpenAIService>();
        services.AddSingleton<GptLoggingService>();
    })
    .Build();

host.Run();
