using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;


namespace Common
{
    public class SsmWrapper
    {
        public static string GetParameter(string parameterName, bool isSecure)
        {
            using var ssmClient = new AmazonSimpleSystemsManagementClient(Amazon.RegionEndpoint.USEast1);
            try
            {
                var response = ssmClient.GetParameterAsync(new GetParameterRequest
                {
                    Name = parameterName,
                    WithDecryption = isSecure
                }).Result;

                return response.Parameter.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error retrieving parameter: {e.Message}");
                return null;
            }
        }

    }
}
