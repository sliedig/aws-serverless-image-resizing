using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ServerlessImageResizing
{
    public class Functions
    {
        private static IAmazonS3 _client;
        private readonly string _bucketName;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            _client = new AmazonS3Client(RegionEndpoint.APSoutheast2);
            _bucketName = Environment.GetEnvironmentVariable("BucketName");
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Get Request\n");
            var sourceUrl = request.QueryStringParameters["source"];
            var mimeType = GetMimeType(sourceUrl);
            string convertedImageUrl;

            using (var client = new HttpClient())
            {
                Task<Stream> s = client.GetStreamAsync(sourceUrl);

                using (Image<Rgba32> image = Image.Load(s.Result))
                {
                    image.Mutate(x => x
                        .Resize(image.Width / 2, image.Height / 2));

                    convertedImageUrl = UploadToS3(image.SavePixelData(), mimeType);
                }
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int) HttpStatusCode.OK,
                Body = convertedImageUrl,
                Headers = new Dictionary<string, string> {{"Content-Type", mimeType}}
            };

            return response;
        }

        private static string GetMimeType(string sourceUrl)
        {
            try
            {
                var ext = Path.GetExtension(sourceUrl).Remove(0);
                return string.Format("image/{0}", ext);
            }
            catch (Exception e)
            {
                return "image/png";
            }
        }

        private string UploadToS3(byte[] imageDataBytes, string mimeType)
        {
            try
            {
                var key = string.Format("{0}", Guid.NewGuid());

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    CannedACL = S3CannedACL.PublicRead,
                    Key = key,
                    ContentType = mimeType
                };

                using (var ms = new MemoryStream(imageDataBytes))
                {
                    request.InputStream = ms;
                    var resp = _client.PutObjectAsync(request);
                    Console.WriteLine(resp.Result.ETag);
                    return string.Format("https://s3-ap-southeast-2.amazonaws.com/serverless-image-resizing-tmp/{0}",
                        key);
                }
            }
            catch (Exception ex)
            {
                // ignored
            }

            return null;
        }
    }
}