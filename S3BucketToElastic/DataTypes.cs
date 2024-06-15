using Elastic.Clients.Elasticsearch.Nodes;
using System;
using System.Collections.Generic;

namespace S3BucketToElastic
{
    public static class DataTypes
    {
        public class UserIdentity
        {
            public string Type { get; set; }
            public string InvokedBy { get; set; }
        }

        public class RequestParameters
        {
            public string BucketName { get; set; }
            public string Host { get; set; }
            public string XAmzAcl { get; set; }
            public string XAmzServerSideEncryption { get; set; }
            public string Key { get; set; }
        }

        public class ResponseElements
        {
            public string XAmzServerSideEncryption { get; set; }
        }

        public class AdditionalEventData
        {
            public string SignatureVersion { get; set; }
            public string CipherSuite { get; set; }
            public int BytesTransferredIn { get; set; }
            public string SSEApplied { get; set; }
            public string AuthenticationMethod { get; set; }
            public string XAmzId2 { get; set; }
            public int BytesTransferredOut { get; set; }
        }

        public class Resource
        {
            public string Type { get; set; }
            public string ARN { get; set; }
            public string AccountId { get; set; }
        }

        public class Event
        {
            public string EventVersion { get; set; }
            public UserIdentity UserIdentity { get; set; }
            public DateTime EventTime { get; set; }
            public string EventSource { get; set; }
            public string EventName { get; set; }
            public string AwsRegion { get; set; }
            public string SourceIPAddress { get; set; }
            public string UserAgent { get; set; }
            public RequestParameters RequestParameters { get; set; }
            public ResponseElements ResponseElements { get; set; }
            public AdditionalEventData AdditionalEventData { get; set; }
            public string RequestID { get; set; }
            public string EventID { get; set; }
            public bool ReadOnly { get; set; }
            public List<Resource> Resources { get; set; }
            public string EventType { get; set; }
            public bool ManagementEvent { get; set; }
            public string RecipientAccountId { get; set; }
            public string SharedEventID { get; set; }
            public string EventCategory { get; set; }
        }

        public class LogFile
        {
            public List<Event> Records { get; set; }
        }
    }
}
