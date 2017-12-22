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
using Amazon.S3.Transfer;
using SixLabors.ImageSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ServerlessImageResizing
{
    public class Functions
    {
        private static IAmazonS3 _client;
        private readonly string _bucketName;
        private const string TempDir = "/tmp/";

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            _client = new AmazonS3Client(RegionEndpoint.APSoutheast2);
            //_bucketName = Environment.GetEnvironmentVariable("S3_BUCKET");
            _bucketName = "serverless-image-resizing-tmp";
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
        
            var resizedImageUrl = ResizeImage(sourceUrl);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int) HttpStatusCode.OK,
                Body = resizedImageUrl,
                Headers = new Dictionary<string, string> {{"Content-Type", "text/plain"}}
            };

            return response;
        }

        private string ResizeImage(string sourceUrl)
        {
            var extension = GetImageExtension(sourceUrl);
            var tempFile = string.Format("{0}.{1}", Guid.NewGuid().ToString("N"), extension);

            using (var client = new HttpClient())
            {
                Task<Stream> s = client.GetStreamAsync(sourceUrl);

                using (Image<Rgba32> image = Image.Load(s.Result))
                {
                    image.Mutate(x => x
                        .Resize(image.Width / 2, image.Height / 2));

                    var filePath = Path.Combine(TempDir, tempFile);

                    image.Save(filePath);

                     return UploadToS3(image.SavePixelData(), filePath, extension);
                }
            }
        }


        private string UploadToS3(byte[] imageDataBytes, string filePath, string extension)
        {
            var key = string.Format("{0}.png", Guid.NewGuid());

            var ms = new MemoryStream(imageDataBytes);

            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                // InputStream = ms,
                BucketName = _bucketName,
                AutoResetStreamPosition = false,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = string.Format("image/{0}", extension),
                Key = key,
                FilePath = filePath
            };


            using (var fileTransferUtility = new TransferUtility(_client))
            {
                fileTransferUtility.Upload(fileTransferUtilityRequest);
            }

            return string.Format("https://s3-{0}.amazonaws.com/{1}/{2}", Environment.GetEnvironmentVariable("AWS_REGION"), _bucketName, key);
        }

        /// <summary>
        /// Returns the extension of the source image
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        private static string GetImageExtension(string sourceUrl)
        {
            try
            {
                return Path.GetExtension(sourceUrl).Remove(0);
            }
            catch (Exception)
            {
                return "png"; // Default to PNG if file extension is missing.
            }
        }
    }
}