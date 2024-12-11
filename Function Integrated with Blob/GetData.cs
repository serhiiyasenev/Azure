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

        public GetData(ILogger<GetData> logger)
        {
            _logger = logger;
        }

        [Function("GetImage")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            const string connectionString = "";
            const string containerName = "images";
            const string blobName = "img.jpg";

            var containerClient = new BlobContainerClient(connectionString, containerName);

            var blobCLient = containerClient.GetBlobClient(blobName);

            var ms = new MemoryStream();

            await blobCLient.DownloadToAsync(ms);

            byte[] imageBytes = ms.ToArray();

            return new FileContentResult(imageBytes, "image/jpg");
        }

        [Function("GetImages")]
        public static async Task<IActionResult> Run1([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            const string connectionString = "";
            const string containerName = "images";

            var containerClient = new BlobContainerClient(connectionString, containerName);
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

            return new JsonResult(result);
        }
    }
}
