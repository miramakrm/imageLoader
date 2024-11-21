using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async context =>
{
    context.Response.Headers["Content-Type"] = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

app.UseStaticFiles();
string uploadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
if (!Directory.Exists(uploadDirectory))
{
    Directory.CreateDirectory(uploadDirectory);
}
app.MapPost("/upload", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var file = form.Files["file"];
    var title = form["title"].ToString();
    if (string.IsNullOrEmpty(title) || file == null)
    {
        context.Response.StatusCode = 400;
        return;
    }
    var fileExtension = Path.GetExtension(file.FileName).ToLower();
    if (fileExtension != ".jpg" && fileExtension != ".png" && fileExtension != ".jpeg" && fileExtension != ".gif")
    {
        context.Response.StatusCode = 400;
        return;
    }
    var imageId = Guid.NewGuid().ToString();
    var filePath = Path.Combine(uploadDirectory, imageId + fileExtension);
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    var imageInfo = new ImageInfo { Id = imageId, Title = title, ImPath = filePath, ImExtension = fileExtension };
    var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images.json");
    List<ImageInfo> images;
    if (File.Exists(jsonFilePath))
    {
        var json = await File.ReadAllTextAsync(jsonFilePath);
        images = JsonSerializer.Deserialize<List<ImageInfo>>(json) ?? new List<ImageInfo>();
    }
    else
    {
        images = new List<ImageInfo>();
    }
    images.Add(imageInfo);
    var updatedJson = JsonSerializer.Serialize(images);
    await File.WriteAllTextAsync(jsonFilePath, updatedJson);


    context.Response.Redirect($"/picture/{imageId}");


});
// displaing image 
app.MapGet("/img/{imageId}", async (HttpContext context) =>
{
    var imageId = context.Request.RouteValues["imageId"]?.ToString();
    if (imageId == null)
    {
        context.Response.StatusCode = 404;
        return;
    }
    var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images.json");
    if (!File.Exists(jsonFilePath))
    {
        context.Response.StatusCode = 404;
        return;
    }
    var json = await File.ReadAllTextAsync(jsonFilePath);
    var images = JsonSerializer.Deserialize<List<ImageInfo>>(json);
    var imageInfo = images?.FirstOrDefault(img => img.Id == imageId);
    if (imageInfo == null)
    {
        context.Response.StatusCode = 404;
        return;
    }

    var imagePath = imageInfo.ImPath;
    var contentType = imageInfo.ImExtension switch
    {
        ".jpg" => "image/jpeg",
        ".png" => "image/png",
        ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        _ => throw new Exception("Unsupported image type"),

    };
    if (!File.Exists(imagePath))
    {
        context.Response.StatusCode = 404;
        return;

    }
    context.Response.ContentType = contentType;
    await using var stream = File.OpenRead(imagePath);
    await stream.CopyToAsync(context.Response.Body);

});
// displaing image details
app.MapGet("/picture/{imageId}", async (HttpContext context) =>
{
    var imageId = context.Request.RouteValues["imageId"]?.ToString();
    if (imageId == null)
    {
        return Results.NotFound();

    }
    var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images.json");
    if (!File.Exists(jsonFilePath))
    {
        return Results.NotFound();
    }
    var json = await File.ReadAllTextAsync(jsonFilePath);
    var images = JsonSerializer.Deserialize<List<ImageInfo>>(json);
    var imageInfo = images?.FirstOrDefault(img => img.Id == imageId);
    if (imageInfo == null)
    {
        return Results.NotFound();
    }
    var html = $@"
<html>
 <head>
     <style>
           body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    margin: 0;
                    padding: 20px;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                }}
        h1, h2 {{
            margin-top : 3vh;
            color: #333;
            text-align: center;
        }}
       img {{
                    max-width: 100%;
                    height: auto;
                    border: 1px solid #ccc;
                    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                    margin-top: 20px;
                }}
                 .container {{
                    background-color: #fff;
                    padding: 20px;
                    border-radius: 8px;
                    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                    text-align: center;
                }}
    </style>
</head>
<body>
    <h2>{imageInfo.Title}</h2>
    <img src='/img/{imageId}' alt='{imageInfo.Title}' width='400'>
</body>
</html>
";

    return Results.Content(html, "text/html");

});
app.Run();
public class ImageInfo
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? ImPath { get; set; }
    public string? ImExtension { get; set; }


}