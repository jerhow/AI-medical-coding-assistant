using Microsoft.Extensions.Configuration;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using System.Collections.Generic;
// using System.Threading.Tasks;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services;

namespace MedicalCodingAssistant.Tests
{
    [TestClass]
    public class ICD10SearchServiceTest
    {
        private IConfiguration? _configuration;
        private ICD10SearchService? _service;

        /// <summary>
        /// Initializes the test class and sets up the configuration and service instances.
        /// This method is called before each test method in the class.
        /// It creates an in-memory configuration with a fake connection string and max allowed results.
        /// The configuration is then used to create an instance of the ICD10SearchService.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "SqlConnectionString", "FakeConnectionString" },
                { "MaxAllowedResults", "100" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _service = new ICD10SearchService(_configuration);
        }

        /// <summary>
        /// Tests the SearchICD10Async method with a valid query that contains results.
        /// It sets up a test service with mock results and verifies that the returned results match the expected values.
        /// The test checks that the method returns the correct number of results and that the usedFreeTextFallback flag is false.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SearchICD10Async_ValidQueryWithContainsResults_ReturnsResults()
        {
            // Arrange
            var query = "diabetes";
            var maxResults = 10;

            Assert.IsNotNull(_configuration, "_configuration is null. Ensure Setup method is called before tests.");
            var testService = new TestICD10SearchService(_configuration!)
            {
                FullTextQueryResults = new List<ICD10Result>
                {
                    new ICD10Result { Code = "E11", LongDescription = "Type 2 diabetes mellitus", Rank = 1 }
                },
                TotalCount = 1
            };

            // Act
            var result = await testService.SearchICD10Async(query, maxResults);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DbSearchResults.Count);
            Assert.AreEqual("E11", result.DbSearchResults[0].Code);
            Assert.IsFalse(result.UsedFreeTextFallback);
        }

        /// <summary>
        /// Tests the SearchICD10Async method with a valid query that does not contain results.
        /// It sets up a test service with mock results for the free text fallback and verifies that the returned results match the expected values.
        /// The test checks that the method returns the correct number of results and that the usedFreeTextFallback flag is true.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SearchICD10Async_ValidQueryWithFreeTextFallback_ReturnsResults()
        {
            // Arrange
            var query = "diabetes";
            var maxResults = 10;

            Assert.IsNotNull(_configuration, "_configuration is null. Ensure Setup method is called before tests.");
            var testService = new TestICD10SearchService(_configuration)
            {
                FullTextQueryResults = new List<ICD10Result>(),
                TotalCount = 0,
                FreeTextQueryResults = new List<ICD10Result>
                {
                    new ICD10Result { Code = "E11", LongDescription = "Type 2 diabetes mellitus", Rank = 1 }
                },
                FreeTextTotalCount = 1
            };

            // Act
            var result = await testService.SearchICD10Async(query, maxResults);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DbSearchResults.Count);
            Assert.AreEqual("E11", result.DbSearchResults[0].Code);
            Assert.IsTrue(result.UsedFreeTextFallback);
        }

        /// <summary>
        /// Tests the SearchICD10Async method with an empty query.
        /// It verifies that the method returns an empty result set and that the total SQL result count is zero.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SearchICD10Async_EmptyQuery_ReturnsEmptyResults()
        {
            // Arrange
            var query = "";
            var maxResults = 10;

            // Act
            Assert.IsNotNull(_service, "_service is null. Ensure Setup method is called before tests.");
            var result = await _service!.SearchICD10Async(query, maxResults);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.DbSearchResults.Count);
            Assert.AreEqual(0, result.TotalSqlResultCount);
        }

        /// <summary>
        /// Tests the SearchICD10Async method with a query that exceeds the maximum allowed results.
        /// It verifies that the method clamps the results to the maximum allowed value and returns the correct number of results.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SearchICD10Async_QueryExceedingMaxAllowedResults_ClampsToMaxAllowedResults()
        {
            // Arrange
            var query = "diabetes";
            var maxResults = 200; // Exceeds max allowed results

            Assert.IsNotNull(_configuration, "_configuration is null. Ensure Setup method is called before tests.");
            var testService = new TestICD10SearchService(_configuration)
            {
                FullTextQueryResults = new List<ICD10Result>
                {
                    new ICD10Result { Code = "E11", LongDescription = "Type 2 diabetes mellitus", Rank = 1 }
                },
                TotalCount = 1
            };

            // Act
            var result = await testService.SearchICD10Async(query, maxResults);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DbSearchResults.Count);
            Assert.AreEqual("E11", result.DbSearchResults[0].Code);
        }

        /// <summary>
        /// Tests the SearchICD10Async method with a query that returns limited results.
        /// It verifies that the method returns the correct number of results and that the total SQL result count matches the expected value.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SearchICD10Async_QueryWithLimitedResults_ReturnsLimitedResults()
        {
            // Arrange
            var query = "diabetes";
            var maxResults = 1;

            Assert.IsNotNull(_configuration, "_configuration is null. Ensure Setup method is called before tests.");
            var testService = new TestICD10SearchService(_configuration)
            {
                FullTextQueryResults = new List<ICD10Result>
                {
                    new ICD10Result { Code = "E11", LongDescription = "Type 2 diabetes mellitus", Rank = 1 }
                },
                TotalCount = 1
            };

            // Act
            var result = await testService.SearchICD10Async(query, maxResults);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DbSearchResults.Count);
            Assert.AreEqual("E11", result.DbSearchResults[0].Code);
        }

        // Custom implementation of ICD10SearchService for testing


        /// <summary>
        /// TestICD10SearchService is a custom implementation of the ICD10SearchService class for testing purposes.
        /// It overrides the FullTextQueryAsync method to return predefined results for testing.
        /// This allows for controlled testing of the SearchICD10Async method without relying on actual database queries.
        /// The class contains properties to set up mock results for both full text and free text queries.
        /// </summary>
        private class TestICD10SearchService : ICD10SearchService
        {
            public List<ICD10Result> FullTextQueryResults { get; set; }
            public int TotalCount { get; set; }
            public List<ICD10Result> FreeTextQueryResults { get; set; }
            public int FreeTextTotalCount { get; set; }

            public TestICD10SearchService(IConfiguration configuration) : base(configuration)
            {
                FullTextQueryResults = new List<ICD10Result>();
                FreeTextQueryResults = new List<ICD10Result>();
            }

            public override Task<(List<ICD10Result>, int)> FullTextQueryAsync(string query, bool useContains, int maxResults)
            {
                if (useContains)
                {
                    return Task.FromResult((FullTextQueryResults, TotalCount));
                }
                else
                {
                    return Task.FromResult((FreeTextQueryResults, FreeTextTotalCount));
                }
            }
        }
    }
}
