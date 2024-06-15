using Amazon;
using Amazon.CloudTrail;
using Amazon.CloudTrail.Model;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Common;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using static S3BucketToElastic.DataTypes;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace S3BucketToElastic
{
    class Program
    {
        static void Main()
        {
            SetCloudTrailIfNeeded();
            IndexBucketFilesToES();
        }

        private static void SetCloudTrailIfNeeded()
        {
            var trailName = Parameters.TrailName;
            var s3BucketName = Parameters.AccessLogsBucketName;

            using var cloudTrailClient = new AmazonCloudTrailClient(RegionEndpoint.USEast1);

            try
            {
                // Check if the trail exists
                var trailsResponse = cloudTrailClient.DescribeTrailsAsync(new DescribeTrailsRequest()).Result;
                var trailExists = trailsResponse.TrailList.Exists(t => t.Name == trailName);

                if (trailExists)
                {
                    return;
                }
                // Create the trail
                var createTrailRequest = new CreateTrailRequest
                {
                    Name = trailName,
                    S3BucketName = s3BucketName,
                    IncludeGlobalServiceEvents = true,
                    IsMultiRegionTrail = true,
                };
                var createTrailResponse = cloudTrailClient.CreateTrailAsync(createTrailRequest).Result;

                // Configure data events
                var putEventSelectorsRequest = new PutEventSelectorsRequest
                {
                    TrailName = trailName,
                    AdvancedEventSelectors = new List<AdvancedEventSelector>
                    {
                        new AdvancedEventSelector()
                        {
                            Name = "Exclude own log bucket",
                            FieldSelectors = new List<AdvancedFieldSelector>
                            {
                                new AdvancedFieldSelector()
                                {
                                    Field = "eventCategory",
                                    Equals = new List<string>{"Data" }
                                },
                                new AdvancedFieldSelector()
                                {
                                    Field = "resources.type",
                                    Equals = new List<string>{"AWS::S3::Object" }
                                },
                                new AdvancedFieldSelector()
                                {
                                    Field = "resources.ARN",
                                    NotStartsWith = new List<string>{"arn:aws:s3:::"+s3BucketName }
                                }
                            }
                        }
                    }
                };

                var reponseFromPut = cloudTrailClient.PutEventSelectorsAsync(putEventSelectorsRequest).Result;

                // Start logging
                var loggingResponse = cloudTrailClient.StartLoggingAsync(new StartLoggingRequest { Name = trailName }).Result;

                Console.WriteLine("CloudTrail is set up and configured successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void IndexBucketFilesToES()
        {
            try
            {
                string bucketName = Parameters.AccessLogsBucketName;
                using (var s3Client = new AmazonS3Client(RegionEndpoint.USEast1))
                {
                    ListObjectsV2Request listRequest = new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                    };

                    ListObjectsV2Response listResponse;
                    do
                    {
                        listResponse = s3Client.ListObjectsV2Async(listRequest).Result;
                        foreach (S3Object entry in listResponse.S3Objects)
                        {
                            try
                            {
                                if (!entry.Key.EndsWith(".json.gz")) continue;

                                // Read log JSON file content
                                GetObjectRequest getRequest = new GetObjectRequest
                                {
                                    BucketName = bucketName,
                                    Key = entry.Key
                                };
                                LogFile logFile;
                                using (GetObjectResponse getResponse = s3Client.GetObjectAsync(getRequest).Result)
                                using (GZipStream decompressionStream = new GZipStream(getResponse.ResponseStream, CompressionMode.Decompress))
                                using (StreamReader reader = new StreamReader(decompressionStream, Encoding.UTF8))
                                {

                                    string jsonContent = reader.ReadToEnd();
                                    logFile = JsonConvert.DeserializeObject<LogFile>(jsonContent);
                                }
                                // Upload Log file to elastic search
                                bool shouldDelete = UploadToElasticsearch(logFile.Records);

                                // Delete the file if it was uploaded successfully
                                if (shouldDelete)
                                {
                                    try
                                    {
                                        DeleteObjectRequest deleteRequest = new DeleteObjectRequest
                                        {
                                            BucketName = bucketName,
                                            Key = entry.Key
                                        };

                                        var deleteResult = s3Client.DeleteObjectAsync(deleteRequest).Result;
                                        Console.WriteLine($"Deleted {entry.Key}");
                                    }
                                    catch (AmazonS3Exception e)
                                    {
                                        Console.WriteLine($"Error deleting {entry.Key}. Message:'{e.Message}'");
                                    }
                                }
                            }
                            catch (AmazonS3Exception e)
                            {
                                Console.WriteLine($"Error reading {entry.Key}. Message:'{e.Message}'");
                            }
                        }
                        listRequest.ContinuationToken = listResponse.NextContinuationToken;
                    } while (listResponse.IsTruncated);
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error listing objects in bucket. Message:'{e.Message}'");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error occurred. Message:'{e.Message}'");
            }
        }

        private static bool UploadToElasticsearch(List<DataTypes.Event> events)
        {
            var cloudId = Parameters.EsCloudId;
            var apiKey = Parameters.ESApiKey;
            var indexName = Parameters.ESIndexName;
            bool isAllSucceeded = true;

            using var settings = new ConnectionSettings(cloudId, new ApiKeyAuthenticationCredentials(apiKey))
                .DefaultIndex(indexName);

            var client = new ElasticClient(settings);

            foreach (var document in events)
            {
                var indexResponse = client.IndexDocumentAsync(document).Result;

                if (indexResponse.IsValid)
                {
                    Console.WriteLine("Document indexed successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to index document.");
                    Console.WriteLine(indexResponse.DebugInformation);
                    isAllSucceeded = false;
                }
            }
            return isAllSucceeded;
        }
    }
}
