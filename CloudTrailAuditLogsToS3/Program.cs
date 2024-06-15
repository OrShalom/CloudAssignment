using Amazon;
using Amazon.CloudTrail;
using Amazon.CloudTrail.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CloudTrailAuditLogsToS3
{
    class Program
    {
        static void Main(string[] args)
        {
            // By Using CloudTrail.LookUpEvents, Get All s3 events
            List<Event> eventsList = null;
            using (AmazonCloudTrailClient ctClient = new AmazonCloudTrailClient(RegionEndpoint.USEast1))
            {
                LookupAttribute lookupAttribute = new LookupAttribute()
                {
                    AttributeKey = LookupAttributeKey.EventSource,
                    AttributeValue = "s3.amazonaws.com"
                };
                LookupEventsRequest request = new LookupEventsRequest()
                {
                    LookupAttributes = new List<LookupAttribute>() { lookupAttribute },
                    StartTime = DateTime.Now.AddHours(-10)
                };
                var response = ctClient.LookupEventsAsync(request).Result;
                eventsList = response.Events;
                string nextToken = response.NextToken;
                while (nextToken != null)
                {
                    request.NextToken = nextToken;
                    response = ctClient.LookupEventsAsync(request).Result;
                    nextToken = response.NextToken;
                    eventsList.AddRange(response.Events);
                }
            }
            // Write the events to another s3 bucket - accesslogs-from-s3
            if (eventsList == null)
            {
                return;
            }
            using (IAmazonS3 s3Client = new AmazonS3Client(RegionEndpoint.USEast1))
            {
                string bucketName = "accesslogs-from-s3";
                string keyName = "AccessLogsFile_" + DateTime.Now.ToShortDateString().Replace('/', '.') +
                    "_" + DateTime.Now.ToShortTimeString().Replace(':', '.');
                var json = JsonConvert.SerializeObject(eventsList);
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentBody = json,
                    ContentType = "application/json"
                };
                var putResponse = s3Client.PutObjectAsync(putRequest).Result;
            }
        }
    }
}
 