
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// 配置支持大文件上传
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000; // 500MB
    options.ValueLengthLimit = 524288000;
    options.MultipartHeadersLengthLimit = 524288000;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500MB
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

var app = builder.Build();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".hash"] = "text/plain";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,

    // 如果需要允许未知扩展（仅调试用），可以启用下面两行
    ServeUnknownFileTypes = true,
    DefaultContentType = "text/plain"
});

app.MapGet("/", () => "Server is running.");

// 添加文件上传接口
app.MapPost("/api/upload", async (HttpRequest request) =>
{
    // 禁用请求体大小限制
    var feature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (feature != null)
    {
        feature.MaxRequestBodySize = null; // 无限制
    }
    try
    {
        if (!request.HasFormContentType || request.Form.Files.Count == 0)
        {
            return Results.BadRequest("没有上传文件");
        }

        var file = request.Form.Files["file"];
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest("文件为空");
        }

        // 获取wwwroot目录路径
        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        // 如果wwwroot目录不存在，创建它
        if (!Directory.Exists(wwwrootPath))
        {
            Directory.CreateDirectory(wwwrootPath);
        }

        // 保存文件到wwwroot目录
        var filePath = Path.Combine(wwwrootPath, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 文件上传成功: {file.FileName} ({file.Length} 字节)");

        return Results.Ok(new { message = "上传成功", fileName = file.FileName, size = file.Length });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 上传失败: {ex.Message}");
        return Results.Problem($"上传失败: {ex.Message}");
    }
});

app.Run();
