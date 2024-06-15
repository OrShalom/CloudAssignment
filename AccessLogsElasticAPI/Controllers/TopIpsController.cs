using Common;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace AccessLogsElasticAPI.Controllers
{
    [ApiController]
    [Route("TopIps")]
    public class TopIpsController : ControllerBase
    {
        private readonly ILogger<TopIpsController> _logger;

        public TopIpsController(ILogger<TopIpsController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "TopMostSourceIPs")]
        public IEnumerable<SourceIpEntry> TopMostSourceIPs(int IPsAmount=3)
        {
            IPsAmount = Math.Max(0, IPsAmount);
            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "Got TopMostSourceIPs request");  
            return GetTopMostFromElastic(IPsAmount);
        }

        IEnumerable<SourceIpEntry> GetTopMostFromElastic(int IPsAmount)
        {
            List<SourceIpEntry> topsMostIpsList = new List<SourceIpEntry>();

            var cloudId = Parameters.EsCloudId;
            var apiKey = Parameters.ESApiKey;
            var indexName = Parameters.ESIndexName;

            var settings = new ConnectionSettings(cloudId, new ApiKeyAuthenticationCredentials(apiKey))
                .DefaultIndex(indexName);

            // Create an ElasticClient instance
            var client = new ElasticClient(settings);

            // Define the aggregation query
            var searchResponse = client.Search<object>(s => s
                .Size(0)
                .Aggregations(a => a
                    .Terms("TopMostIps", t => t
                        .Field("sourceIPAddress.enum")
                    )
                )
            );

            // Check if the query was successful
            if (searchResponse.IsValid)
            {
                var topMostIps = searchResponse.Aggregations.Terms("TopMostIps");

                // Output the results
                foreach (var bucket in topMostIps.Buckets)
                {
                    _logger.LogInformation($"IP: {bucket.Key}, Count: {bucket.DocCount}");
                    var newEntry = new SourceIpEntry() 
                    { 
                        IpAdress = bucket.Key, 
                        Amount = bucket.DocCount 
                    };
                    topsMostIpsList.Add(newEntry);
                }
            }
            else
            {
                _logger.LogInformation("Query failed.");
                _logger.LogInformation(searchResponse.DebugInformation);
            }
            return topsMostIpsList.GetRange(0, Math.Min(topsMostIpsList.Count,IPsAmount));
        }
    }
}
