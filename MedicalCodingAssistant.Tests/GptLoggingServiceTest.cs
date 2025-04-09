using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MedicalCodingAssistant.Tests.Services
{
    [TestClass]
    public class GptLoggingServiceTest
    {
        private IConfiguration _configuration = null!;
        private TestLogger<GptLoggingService> _testLogger = null!;
        private GptLoggingService _loggingService = null!;
        private string _tempLogFilePath = null!;

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

        [TestCleanup]
        public void Cleanup()
        {
            // Delete the temporary log file after tests
            if (File.Exists(_tempLogFilePath))
            {
                File.Delete(_tempLogFilePath);
            }
        }

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

        // TestLogger implementation for capturing log messages
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
}