using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var sqlConnectionString = builder.Configuration.GetConnectionString("SqlDatabase");
var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseHttpsRedirection();

app.MapGet("/test", () =>
{
    var constants = new (string Name, double Value)[]
    {
        ("π (Pi)", Math.PI),
        ("e (Euler)", Math.E),
        ("√2 (Sqrt 2)", Math.Sqrt(2)),
        ("φ (Golden Ratio)", (1 + Math.Sqrt(5)) / 2),
        ("ln(2) (Log 2)", Math.Log(2)),
        ("ln(10) (Log 10)", Math.Log(10))
    };

    int maxNameLength = constants.Max(c => c.Name.Length);

    return string.Join(Environment.NewLine,
        constants.Select(c => $"{c.Name.PadRight(maxNameLength)} : {c.Value}"));
})
.WithName("test")
.WithOpenApi();

app.MapGet("/database", async (HttpContext context) =>
{
    var users = new List<object>();

    using (var connection = new SqlConnection(sqlConnectionString))
    {
        await connection.OpenAsync();
        using var command = new SqlCommand("SELECT UserId, UserName, Email, DateOfBirth, IsActive FROM Users", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new
            {
                UserId = reader.GetInt32(0),
                UserName = reader.GetString(1),
                Email = reader.GetString(2),
                DateOfBirth = reader.GetDateTime(3),
                IsActive = reader.GetBoolean(4)
            });
        }
    }

    var html = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Users Table</title>
    <style>
        table {
            width: 100%;
            border-collapse: collapse;
        }
        th, td {
            border: 1px solid #ddd;
            padding: 8px;
        }
        th {
            background-color: #f4f4f4;
            text-align: left;
        }
    </style>
</head>
<body>
    <h1>Users Table</h1>
    <table>
        <thead>
            <tr>
                <th>UserId</th>
                <th>UserName</th>
                <th>Email</th>
                <th>DateOfBirth</th>
                <th>IsActive</th>
            </tr>
        </thead>
        <tbody>";

    foreach (var user in users)
    {
        html += $@"
            <tr>
                <td>{user.GetType().GetProperty("UserId")?.GetValue(user)}</td>
                <td>{user.GetType().GetProperty("UserName")?.GetValue(user)}</td>
                <td>{user.GetType().GetProperty("Email")?.GetValue(user)}</td>
                <td>{((DateTime)user.GetType().GetProperty("DateOfBirth")?.GetValue(user)).ToString("yyyy-MM-dd")}</td>
                <td>{((bool)user.GetType().GetProperty("IsActive")?.GetValue(user) ? "Active" : "Inactive")}</td>
            </tr>";
    }

    html += @"
        </tbody>
    </table>
</body>
</html>";

    return Results.Content(html, "text/html");
})
.WithName("Database")
.WithOpenApi();

app.MapGet("/blob", async () =>
{
    const string containerName = "files";
    const string blobName = "TEXT.txt";

    var blobServiceClient = new BlobServiceClient(blobConnectionString);
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    var blobClient = containerClient.GetBlobClient(blobName);

    var localFilePath = Path.Combine(Path.GetTempPath(), blobName);
    await blobClient.DownloadToAsync(localFilePath);

    string[] lines = File.ReadAllLines(localFilePath);
    return lines;
})
.WithName("Blob")
.WithOpenApi();

app.Run();

