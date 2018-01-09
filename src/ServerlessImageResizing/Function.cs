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
using Amazon.S3.Transfer;
using SixLabors.ImageSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ServerlessImageResizing
{
    public class Functions
    {
        private static IAmazonS3 _s3Client;
        private readonly string _s3BucketName;
        private readonly string _awsRegion;
        private const string TempDir = "/tmp/";

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            _awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(_awsRegion));
            _s3BucketName = Environment.GetEnvironmentVariable("S3_BUCKET");
            
            // Todo: check to see if the S3 bucket exists. If not create it.
        }

        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var sourceUrl = request.QueryStringParameters["source"];
            context.Logger.LogLine(string.Format("Received request to convert source image: {0}", sourceUrl));
            
            APIGatewayProxyResponse response;
            
            if (sourceUrl != null)
            {
                var resizedImageUrl = ResizeImage(sourceUrl);

                response = new APIGatewayProxyResponse
                {
                    StatusCode = (int) HttpStatusCode.OK,
                    Body = resizedImageUrl,
                    Headers = new Dictionary<string, string> {{"Content-Type", "text/plain"}}
                };    
            }
            else
            {
                response = new APIGatewayProxyResponse
                {
                    StatusCode = (int) HttpStatusCode.BadRequest,
                    Body = "Unable to process file. File name is empty.",
                    Headers = new Dictionary<string, string> {{"Content-Type", "text/plain"}}
                };
            }
            
            return response;
        }

        /// <summary>
        /// Resize operation
        /// </summary>
        /// <param name="sourceUrl">source file url</param>
        /// <returns></returns>
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

                     return UploadToS3(filePath, extension);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">location of resized file in tmp storage</param>
        /// <param name="extension">file extension</param>
        /// <returns>Url of the object stored in S3 bucket.</returns>
        private string UploadToS3(string filePath, string extension)
        {
            var key = string.Format("{0}.png", Guid.NewGuid());
            
            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                // InputStream = ms,
                BucketName = _s3BucketName,
                AutoResetStreamPosition = false,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = string.Format("image/{0}", extension),
                Key = key,
                FilePath = filePath
            };

            using (var fileTransferUtility = new TransferUtility(_s3Client))
            {
                fileTransferUtility.Upload(fileTransferUtilityRequest);
            }

            return string.Format("https://s3-{0}.amazonaws.com/{1}/{2}", _awsRegion, _s3BucketName, key);
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