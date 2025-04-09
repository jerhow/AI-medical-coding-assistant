using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services;

namespace MedicalCodingAssistant.Tests.Services;

/// <summary>
/// Unit tests for the GptLoggingService class
/// </summary>
[TestClass]
public class GptLoggingServiceTest
{
    private IConfiguration _configuration = null!;
    private TestLogger<GptLoggingService> _testLogger = null!;
    private GptLoggingService _loggingService = null!;
    private string _tempLogFilePath = null!;

    /// <summary>
    /// Initializes the test class and sets up the configuration and service instances.
    /// This method is called before each test method in the class.
    /// It creates an in-memory configuration with logging settings and a test logger.
    /// The GptLoggingService is instantiated with these settings.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Create in-memory configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "EnableGptConsoleLogging", "true" },
            { "EnableGptFileLogging", "true" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Create a test logger
        _testLogger = new TestLogger<GptLoggingService>();

        // Set up a temporary log file path
        _tempLogFilePath = Path.GetTempFileName();

        // Override the log file path in the configuration
        _loggingService = new GptLoggingService(_testLogger, _configuration)
        {
            // Inject a temporary log file path for testing
            _logFilePath = _tempLogFilePath
        };
    }

    /// <summary>
    /// Cleans up the test class after all tests have run.
    /// This method is called after each test method in the class.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        // Delete the temporary log file after tests
        if (File.Exists(_tempLogFilePath))
        {
            File.Delete(_tempLogFilePath);
        }
    }

    /// <summary>
    /// Tests the Log method of GptLoggingService.
    /// It verifies that the log message is correctly logged to the console and the file.
    /// </summary>
    [TestMethod]
    public void Log_LogsToConsole()
    {
        // Arrange
        var log = new GptResponseLog
        {
            // We could add these to `GptResponseLog` for testing, but they are not crucial and would clutter up the model.
            // Request = "Test request",
            // Response = "Test response",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _loggingService.Log(log);

        // Assert
        Assert.IsTrue(_testLogger.LogMessages.Contains($"GPT Response Log:\n{JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}"));
    }

    /// <summary>
    /// Tests the Log method of GptLoggingService.
    /// </summary>
    [TestMethod]
    public void Log_LogsToFile()
    {
        // Arrange
        var log = new GptResponseLog
        {
            // We could add these to `GptResponseLog` for testing, but they are not crucial and would clutter up the model.
            // Request = "Test request",
            // Response = "Test response",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _loggingService.Log(log);

        // Assert
        Assert.IsTrue(File.Exists(_tempLogFilePath));
        var fileContent = File.ReadAllText(_tempLogFilePath);
        Assert.IsTrue(fileContent.Contains(JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })));
    }

    /// <summary>
    /// Test logger class for capturing log messages.
    /// Implements the ILogger<T> interface and stores log messages in a list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class TestLogger<T> : ILogger<T>
    {
        public List<string> LogMessages { get; } = new();

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogMessages.Add(formatter(state, exception));
        }
    }
}
