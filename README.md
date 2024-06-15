# Cloud Assignment
## Overview
This project was developed as part of a solution to an assignment that requires audit access to S3 buckets using AWS CloudTrail, ingest the audit logs into Elasticsearch, visualize the data in Kibana, and provide an API to retrieve the top source IPs that have accessed the S3 buckets.

## The Assignment
1. We want to audit an S3 bucket access using AWS CloudTrail.
    Send the audit logs to another S3 bucket.
2. Ingest the logs from the bucket into ElasticSearch.
    You can run ElasticSearch and Kibana locally on your PC or in AWS (in EC2, Kubernetes or as AWS OpenSearch).
3. Create a visualization in Kibana that shows PutObject request by the source IP.
4. Create an API using C# to return GET for top most source IPs (it’ll need to query the ElasticSearch for that of course).
    Consider adding Continues Integration with tests.


# Solution 
I used my own AWS account, using Lambda function for the main app, CloudTrail for auditing logs, S3 for storage, and Parameter Store (ssm) for secrets and parameters storage.

For ElasticSearch I used Elastic Cloud with a free trial for 2 weeks.

### Security Considerations
1. Because my code runs on AWS, all the authentication to S3 and all the other services is native and secure.
2. Access to ElasticCloud is by using Cloud Id and an API key. Both are saved in the Parameter Store in my AWS account.

## *S3BucketToElastic Project* - Audit S3 bucket access using AWS CloudTrail + Index events into ElasticSearch
I decided to solve this step using CloudTrail trail feature, which can automatically upload all the audit logs into a bucket. 
The trail should be created only once, and it assures us that all the access logs of S3 will be uploaded into an S3 bucket (except the access logs of the bucket we are using to upload the logs to).
I chose to run the code on AWS Lambda.
#### The flow:
1. Making sure we have the trail up and running (if not, create it).
2. Fetching the log files from the chosen S3 bucket.
3. Upload those logs into the Elastic Search index.

#### Environment Variables
I used the following Environment Variables (with default values) in order to change parameters for your use:
* `CloudTrailName` - The name of the trail that we created.
* `AccessLogsBucketName` - The name of the bucket we want to upload the log files to.
* `ESIndexName` - The name of the index in ElasticSearch.
* `EsCloudIdParameterName` - The Cloud Id where our Elastic Search is deployed. This parameter is being fetched from the AWS Parameter Store, and this is the name of the parameter.
* `ESApiKeyParameterName` - The Elastic Cloud API Key. This parameter is being fetched from the AWS Parameter Store, and this is the name of the parameter.

## Create Visualisation in Kibana - PutObject request by the source IP
All the data in the ElasticSearch index is mapped dynamically.

By using Kibana, I create a Dashboard with Visualization and define sourceIPAddress.enum as a Horizontal axis and the count of records of eventName.enum as a Vertical axis.

Then I added a filter to show only PutObject event from eventName.enum. 

[Link for the dashboard (need to be logged into ElasticCloud\ElasticSearch)](https://23d36bee13334be9a2bb1c5918d46e53.us-east-1.aws.found.io:9243/app/dashboards#/view/2585eefb-1714-47cb-87ec-d8c6976a5d3f?_g=(refreshInterval%3A(pause%3A!t%2Cvalue%3A60000)%2Ctime%3A(from%3Anow-6w%2Cto%3Anow%2Fw)))

The result:
![image](https://github.com/OrShalom/CloudAssignment/assets/73779420/c6f804ae-dd0f-4f2f-9187-c22d8c4d3bd8)

## *AccessLogsElasticAPI Project* - ASP.NET API with GET for top most source IPs 
I created a simple web API using ASP.NET Core which implements only 1 controller with 1 GET request.

This project includes OpenApi integration already, so running this locally will open OpenApi dashboard in order to try this API.

The Request: 
#### Get TopMostSourceIPs
```http
  GET /TopIps 
```
Or another example to return top 5 most used source ips:
```http
  GET /TopIps?IPsAmount=5
```

| Parameter | Type     | Description                               | Required    |
| :-------- | :------- | :---------------------------------------- | :-----------|
| `IPsAmount` | `int` | The amount of top ips to return. Default = 3 | False 

 Open API dashboard:
 ![image](https://github.com/OrShalom/CloudAssignment/assets/73779420/7a354f22-0698-4bb8-8bbe-e5f637a498f5)


## *CloudTrailAuditLogsToS3 Project* - First try auditing access logs
At first, I tried to use LookupEvents (CloudTrail API) for the events that are captured by CloudTrail and upload them myself into an S3 bucket.
After I finished implementing this, I went over the log files and couldn't find *putObject* events.

Then I found out that i couldn't use this API in order to get *Data events* such as *putObject*.

So finally, I ditched this solution😊






 

 
