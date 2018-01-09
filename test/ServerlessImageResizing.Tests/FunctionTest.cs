using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using ServerlessImageResizing;

namespace ServerlessImageResizing.Tests
{
    public class FunctionTest
    {
        public FunctionTest()
        {
        }

        [Fact]
        public void TestGetMethod()
        {
            var request = new APIGatewayProxyRequest
            {
                QueryStringParameters = new Dictionary<string, string>
                {
                    {"source", "http://via.placeholder.com/500x300"}
                }
            };

            var functions = new Functions();
            var context = new TestLambdaContext();
            
            var response = functions.Get(request, context);
            
            Assert.Equal(200, response.StatusCode);
            Assert.StartsWith("https://s3", response.Body);
        }
    }
}
