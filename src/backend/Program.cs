using UnWeb.Services;
using UnWeb.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

	options.AddPolicy("all", policy =>
	{
		policy.WithOrigins("*")
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

builder.Services.AddScoped<IConversionService, ConversionService>();
builder.Services.AddHttpClient<IConversionService, ConversionService>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevPolicy");
}
else
{
    app.UseCors("all");
}

// API Endpoints
    app.MapPost("/api/convert/paste", async (ConvertRequest request, IConversionService conversionService) =>
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required" });
        }

        try
        {
            var result = await conversionService.ConvertHtmlToMarkdownAsync(request.Html);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Conversion failed"
            );
        }
    });

app.MapPost("/api/convert/upload", async (IFormFile file, IConversionService conversionService) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "No file uploaded" });
    }

    // Validate file extension
    var allowedExtensions = new[] { ".html", ".htm" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(extension))
    {
        return Results.BadRequest(new { error = "Only .html and .htm files are allowed" });
    }

    // Validate file size (5MB max)
    const long maxFileSize = 5 * 1024 * 1024;
    if (file.Length > maxFileSize)
    {
        return Results.StatusCode(413); // Payload Too Large
    }

    try
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var html = await reader.ReadToEndAsync();

        var result = await conversionService.ConvertHtmlToMarkdownAsync(html);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Conversion failed"
        );
    }
})
.DisableAntiforgery();

app.MapPost("/api/convert/url", async (UrlConvertRequest request, IConversionService conversionService) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new { error = "URL is required" });
    }

    try
    {
        var result = await conversionService.ConvertUrlToMarkdownAsync(request.Url);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        // Map different error types to appropriate status codes
        var message = ex.Message.ToLower();

        if (message.Contains("invalid url") || message.Contains("unsupported protocol"))
            return Results.BadRequest(new { error = ex.Message });

        if (message.Contains("private ip") || message.Contains("not allowed"))
            return Results.StatusCode(403); // Forbidden

        if (message.Contains("timeout"))
            return Results.StatusCode(504); // Gateway Timeout

        if (message.Contains("too large"))
            return Results.StatusCode(413); // Payload Too Large

        if (message.Contains("content type") || message.Contains("unsupported"))
            return Results.StatusCode(415); // Unsupported Media Type

        // Default to Bad Gateway for network/fetch errors
        return Results.Problem(
            detail: ex.Message,
            statusCode: 502,
            title: "Failed to fetch URL"
        );
    }
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Make Program accessible for testing
public partial class Program { }
