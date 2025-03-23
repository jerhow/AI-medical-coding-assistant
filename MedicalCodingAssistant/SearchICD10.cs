using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCodingAssistant
{
    public class SearchICD10
    {
        private readonly ILogger<SearchICD10> _logger;

        public SearchICD10(ILogger<SearchICD10> logger)
        {
            _logger = logger;
        }

        [Function("SearchICD10")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
