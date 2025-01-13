using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Function;

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
    [OpenApiOperation(operationId: "GetText", ["GetData"])]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Return a simple text")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Bad request")]
    public IActionResult GetText([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, bool error = false)
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
    [OpenApiOperation(operationId: "GetImage", ["GetData"])]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "image/jpg", bodyType: typeof(byte[]), Description = "Returns JPG image")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Something went wrong")]
    public async Task<IActionResult> GetImage([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
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
    [OpenApiOperation(operationId: "GetImages", ["GetData"])]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<object>), Description = "Returns the list of blob metadata.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest,Description = "Bad request")]
    public async Task<IActionResult> GetImages([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        const string containerName = "images";

        var containerClient = new BlobContainerClient(_connectionString, containerName);
        var blobs = containerClient.GetBlobsAsync();

        var blobMetadataList = new List<object>();

        await foreach (var blobItem in blobs)
        {
            blobMetadataList.Add(new
            {
                blobItem.Name,
                LastModifiedUtc = blobItem.Properties.LastModified,
                AccessTier = blobItem.Properties.AccessTier?.ToString(),
                ArchiveStatus = blobItem.Properties.ArchiveStatus?.ToString(),
                BlobType = blobItem.Properties.BlobType?.ToString(),
                SizeInKiB = Math.Round((decimal)(blobItem.Properties.ContentLength / 1024.00), 2),
                LeaseState = blobItem.Properties.LeaseState?.ToString()
            });
        }

        _logger.LogInformation("Return images JsonResult");

        return new JsonResult(blobMetadataList);
    }
}
