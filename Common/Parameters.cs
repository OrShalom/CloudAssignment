namespace Common
{
    public static class Parameters
    {
        public static string EsCloudId  => SsmWrapper.GetParameter(Environment.GetEnvironmentVariable("EsCloudIdParameterName") ?? Constants.EsCloudIdParameterName, false);
        public static string ESApiKey => SsmWrapper.GetParameter(Environment.GetEnvironmentVariable("ESApiKeyParameterName") ?? Constants.ESApiKeyParameterName, true);
        public static string ESIndexName => Environment.GetEnvironmentVariable("ESIndexName") ?? Constants.AccessLogESIndexName;
        public static string AccessLogsBucketName => Environment.GetEnvironmentVariable("AccessLogsBucketName") ?? Constants.BucketName;
        public static string TrailName => Environment.GetEnvironmentVariable("CloudTrailName") ?? Constants.CloudTrailName;
    }
}
