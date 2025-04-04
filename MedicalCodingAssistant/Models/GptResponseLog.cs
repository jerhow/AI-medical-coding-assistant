namespace MedicalCodingAssistant.Models;

public class GptResponseLog
{
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Query { get; set; } = string.Empty;
        public int SqlResultCount { get; set; }
        public List<ICD10Result> SqlResults { get; set; } = new();
        public string GptResponseJson { get; set; } = string.Empty;

        // Additional AI metadata
        public string ApiVersion { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string? SystemPrompt { get; set; }
        public string? UserPrompt { get; set; }

        // Optional future fields
        public string? DeploymentName { get; set; }
        public string? Environment { get; set; } // dev/staging/prod

        // Token counts
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
}
