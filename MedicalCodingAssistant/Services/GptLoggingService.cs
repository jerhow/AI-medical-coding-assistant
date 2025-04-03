using MedicalCodingAssistant.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace MedicalCodingAssistant.Services;

public class GptLoggingService
{
    private readonly ILogger<GptLoggingService> _logger;
    private readonly bool _enabled;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GptLoggingService(ILogger<GptLoggingService> logger, IConfiguration config)
    {
        _logger = logger;
        _enabled = config.GetValue<bool>("EnableGptLogging");
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Don't escape unicode characters (only do this in dev - we'll need configuration for upstream environments)
        };
    }

    public void Log(GptResponseLog log)
    {
        if (!_enabled) return;

        // _logger.LogInformation("GPT Response Log: {@Log}", log); // Use this for upstream environments
        _logger.LogInformation("GPT Response Log:\n{Json}", JsonSerializer.Serialize(log, _jsonSerializerOptions)); // Use this for local development (config this later)
    }
}
