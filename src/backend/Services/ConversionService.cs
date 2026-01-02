using AngleSharp;
using AngleSharp.Dom;
using ReverseMarkdown;
using UnWeb.Models;

namespace UnWeb.Services;

public class ConversionService : IConversionService
{
    private readonly ILogger<ConversionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Converter _markdownConverter;

    public ConversionService(ILogger<ConversionService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;

        // Configure HttpClient
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "UnWeb/1.0 (HTML to Markdown Converter)");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);

        // Configure ReverseMarkdown for basic CommonMark
        var config = new ReverseMarkdown.Config
        {
            UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass,
            GithubFlavored = false,
            RemoveComments = true,
            SmartHrefHandling = true
        };

        _markdownConverter = new Converter(config);
    }

    public async Task<ConvertResponse> ConvertHtmlToMarkdownAsync(string html)
    {
        var warnings = new List<string>();

        try
        {
            // Parse HTML with AngleSharp
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html));

            // Extract main content
            var contentElement = ExtractMainContent(document, warnings);

            // Convert to markdown
            var markdown = _markdownConverter.Convert(contentElement.OuterHtml);

            // Clean up markdown (remove excessive newlines)
            markdown = CleanMarkdown(markdown);

            return new ConvertResponse(markdown, warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting HTML to markdown");
            throw new InvalidOperationException("Failed to convert HTML to markdown", ex);
        }
    }

    public async Task<ConvertResponse> ConvertUrlToMarkdownAsync(string url)
    {
        try
        {
            // Validate URL
            ValidateUrl(url);
            _logger.LogInformation("Fetching HTML from URL: {Url}", url);

            // Fetch HTML from URL
            var html = await FetchHtmlFromUrlAsync(url);
            _logger.LogInformation("Successfully fetched {Length} bytes from {Url}", html.Length, url);

            // Reuse existing conversion pipeline
            return await ConvertHtmlToMarkdownAsync(html);
        }
        catch (InvalidOperationException)
        {
            // Re-throw validation/fetch errors as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting URL to markdown: {Url}", url);
            throw new InvalidOperationException("Failed to convert URL to markdown", ex);
        }
    }

    private IElement ExtractMainContent(IDocument document, List<string> warnings)
    {
        // Try semantic HTML5 tags first
        var mainElement = document.QuerySelector("main")
                         ?? document.QuerySelector("article")
                         ?? document.QuerySelector("[role='main']");

        if (mainElement != null)
        {
            _logger.LogInformation("Main content extracted using semantic HTML");
            return mainElement;
        }

        // Fallback: Try to find content-rich element
        var candidateElements = document.QuerySelectorAll("div");
        IElement? bestCandidate = null;
        int maxScore = 0;

        foreach (var element in candidateElements)
        {
            int score = CalculateContentScore(element);
            if (score > maxScore)
            {
                maxScore = score;
                bestCandidate = element;
            }
        }

        if (bestCandidate != null && maxScore > 100)
        {
            _logger.LogInformation("Main content extracted using content analysis");
			return bestCandidate;
        }

        // Last resort: use body
        _logger.LogWarning("No main content detected; using entire body");
		return document.Body ?? document.DocumentElement;
    }

    private int CalculateContentScore(IElement element)
    {
        // Remove scripts, styles, nav, footer, aside
        var textContent = element.TextContent;
        var score = 0;

        // Check if element contains excluded tags
        if (element.QuerySelector("nav, footer, aside, script, style") != null)
        {
            return 0;
        }

        // Score based on text length
        score += Math.Min(textContent.Length / 10, 100);

        // Score based on paragraph count
        var paragraphs = element.QuerySelectorAll("p");
        score += paragraphs.Length * 10;

        // Penalty for high link density
        var links = element.QuerySelectorAll("a");
        if (links.Length > paragraphs.Length * 2)
        {
            score /= 2;
        }

        return score;
    }

    private string CleanMarkdown(string markdown)
    {
        // Remove excessive newlines (more than 2 consecutive)
        var lines = markdown.Split('\n');
        var cleaned = new List<string>();
        int consecutiveEmpty = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveEmpty++;
                if (consecutiveEmpty <= 2)
                {
                    cleaned.Add(line);
                }
            }
            else
            {
                consecutiveEmpty = 0;
                cleaned.Add(line);
            }
        }

        return string.Join('\n', cleaned).Trim();
    }

    private void ValidateUrl(string url)
    {
        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Invalid URL format");
        }

        // Only allow HTTP/HTTPS
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            throw new InvalidOperationException($"Unsupported protocol: {uri.Scheme}. Only HTTP and HTTPS are allowed");
        }

        // Block private IP ranges (SSRF prevention)
        var host = uri.Host;
        if (IsPrivateOrLocalhost(host))
        {
            throw new InvalidOperationException("Access to private IP addresses is not allowed");
        }
    }

    private bool IsPrivateOrLocalhost(string host)
    {
        // Check for localhost
        if (host == "localhost" || host == "127.0.0.1" || host == "0.0.0.0")
            return true;

        // Try to parse as IP address
        if (System.Net.IPAddress.TryParse(host, out var ipAddress))
        {
            var bytes = ipAddress.GetAddressBytes();

            // Check private ranges
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 127.0.0.0/8 (loopback)
            if (bytes[0] == 127)
                return true;
        }

        return false;
    }

    private async Task<string> FetchHtmlFromUrlAsync(string url)
    {
        const long maxContentSize = 10 * 1024 * 1024; // 10MB

        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            // Check HTTP status
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to fetch URL. Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            // Validate Content-Type
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != "text/html" && !contentType?.StartsWith("text/html") == true)
            {
                throw new InvalidOperationException(
                    $"Unsupported content type: {contentType}. Only text/html is supported");
            }

            // Check content length
            if (response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength.Value > maxContentSize)
            {
                throw new InvalidOperationException(
                    $"Content too large: {response.Content.Headers.ContentLength.Value} bytes. Maximum: {maxContentSize} bytes");
            }

            // Read content with size limit
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var html = await reader.ReadToEndAsync();

            // Final size check (in case Content-Length header was missing)
            if (html.Length > maxContentSize)
            {
                throw new InvalidOperationException(
                    $"Content too large: {html.Length} bytes. Maximum: {maxContentSize} bytes");
            }

            return html;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching URL: {Url}", url);
            throw new InvalidOperationException($"Failed to fetch URL: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching URL: {Url}", url);
            throw new InvalidOperationException("Request timed out after 60 seconds", ex);
        }
    }
}
