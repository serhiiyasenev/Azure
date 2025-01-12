using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public class GetData
    {
        private readonly ILogger<GetData> _logger;
        private readonly string _connectionString;

        public GetData(ILogger<GetData> logger)
        {
            _logger = logger;
            _connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
        }

        [Function("GetText")]
        public IActionResult RunText([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, bool error = false)
        {
            if (error)
            {
                _logger.LogInformation("An error occurred");
                return new BadRequestObjectResult("An error occurred");
            }

            _logger.LogInformation("Get text finished");
            return new OkObjectResult("Hello World");
        }


        [Function("GetImage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            const string containerName = "images";
            const string blobName = "img.jpg";

            var containerClient = new BlobContainerClient(_connectionString, containerName);

            var blobCLient = containerClient.GetBlobClient(blobName);

            var ms = new MemoryStream();

            await blobCLient.DownloadToAsync(ms);

            byte[] imageBytes = ms.ToArray();

            _logger.LogInformation("Return image");

            return new FileContentResult(imageBytes, "image/jpg");
        }

        [Function("GetImages")]
        public async Task<IActionResult> Run1([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            const string containerName = "images";

            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var blobs = containerClient.GetBlobsAsync();

            var imageFiles = new List<(string FileName, byte[] Content, string ContentType)>();

            await foreach (var blob in blobs)
            {
                var blobClient = containerClient.GetBlobClient(blob.Name);

                var ms = new MemoryStream();
                await blobClient.DownloadToAsync(ms);
                ms.Position = 0;

                string contentType = blob.Properties.ContentType ?? "application/octet-stream";
                imageFiles.Add((blob.Name, ms.ToArray(), contentType));
            }

            var result = imageFiles.Select(image => new
            {
                FileName = image.FileName,
                ContentType = image.ContentType,
                Base64Content = Convert.ToBase64String(image.Content)
            });

            _logger.LogInformation("Return images JsonResult");

            return new JsonResult(result);
        }
    }
}
