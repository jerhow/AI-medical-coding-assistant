using MedicalCodingAssistant.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace MedicalCodingAssistant.Services;

public class GptLoggingService
{
    private readonly ILogger<GptLoggingService> _logger;
    private readonly bool _logToConsole;
    private readonly bool _logToFile;
    private readonly string? _logFilePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GptLoggingService(ILogger<GptLoggingService> logger, IConfiguration config)
    {
        _logger = logger;
        _logToConsole = config.GetValue<bool>("EnableGptConsoleLogging");
        _logToFile = config.GetValue<bool>("EnableGptFileLogging");

        if (_logToFile)
        {
            var root = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            var logsDir = Path.Combine(root ?? ".", "Logs");
            Directory.CreateDirectory(logsDir);
            _logFilePath = Path.Combine(logsDir, $"gpt-log-{DateTime.UtcNow:yyyyMMdd}.jsonl");
        }

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Don't escape unicode characters (only do this in dev - we'll need configuration for upstream environments)
        };
    }

    public void Log(GptResponseLog log)
    {
        var json = JsonSerializer.Serialize(log, _jsonSerializerOptions);

        if (_logToConsole)
        {
            _logger.LogInformation("GPT Response Log:\n{Json}", json);
        }

        if (_logToFile && _logFilePath is not null)
        {
            File.AppendAllText(_logFilePath, json + Environment.NewLine);
        }
    }
}
