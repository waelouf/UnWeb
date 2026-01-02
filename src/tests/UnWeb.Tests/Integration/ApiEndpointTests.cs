using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using UnWeb.Models;
using Xunit;

namespace UnWeb.Tests.Integration;

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task ConvertPaste_WithValidHtml_ReturnsMarkdown()
    {
        // Arrange
        var request = new ConvertRequest("<h1>Test</h1><p>Content here.</p>");

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/paste", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result.Should().NotBeNull();
        result!.Markdown.Should().Contain("# Test");
        result.Markdown.Should().Contain("Content here.");
    }

    [Fact]
    public async Task ConvertPaste_WithEmptyHtml_ReturnsBadRequest()
    {
        // Arrange
        var request = new ConvertRequest("");

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/paste", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertPaste_WithComplexHtml_ExtractsMainContent()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <nav><a href='/'>Nav</a></nav>
                    <main>
                        <h1>Article Title</h1>
                        <p>Main content paragraph.</p>
                        <ul>
                            <li>Item 1</li>
                            <li>Item 2</li>
                        </ul>
                    </main>
                    <footer>Footer content</footer>
                </body>
            </html>";
        var request = new ConvertRequest(html);

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/paste", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result.Should().NotBeNull();
        result!.Markdown.Should().Contain("# Article Title");
        result.Markdown.Should().Contain("Main content paragraph.");
        result.Markdown.Should().NotContain("Nav");
        result.Markdown.Should().NotContain("Footer content");
        result.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ConvertUpload_WithValidHtmlFile_ReturnsMarkdown()
    {
        // Arrange
        var htmlContent = "<main><h1>Uploaded Content</h1><p>From file.</p></main>";
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(htmlContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        content.Add(fileContent, "file", "test.html");

        // Act
        var response = await _client.PostAsync("/api/convert/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result.Should().NotBeNull();
        result!.Markdown.Should().Contain("# Uploaded Content");
        result.Markdown.Should().Contain("From file.");
    }

    [Fact]
    public async Task ConvertUpload_WithInvalidExtension_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("<h1>Test</h1>"));
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await _client.PostAsync("/api/convert/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertUpload_WithNoFile_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync("/api/convert/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertUpload_WithHtmExtension_ReturnsOk()
    {
        // Arrange
        var htmlContent = "<h1>HTM File</h1>";
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(htmlContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        content.Add(fileContent, "file", "test.htm");

        // Act
        var response = await _client.PostAsync("/api/convert/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConvertPaste_WithLinksAndImages_ConvertsCorrectly()
    {
        // Arrange
        var html = @"
            <main>
                <p>Visit <a href='https://example.com'>our website</a>.</p>
                <img src='logo.png' alt='Company Logo' />
            </main>";
        var request = new ConvertRequest(html);

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/paste", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result!.Markdown.Should().Contain("[our website](https://example.com)");
        result.Markdown.Should().Contain("![Company Logo](logo.png)");
    }

    [Fact]
    public async Task ConvertPaste_WithMultipleHeadings_PreservesHierarchy()
    {
        // Arrange
        var html = @"
            <main>
                <h1>Title</h1>
                <h2>Subtitle</h2>
                <h3>Section</h3>
                <p>Content</p>
            </main>";
        var request = new ConvertRequest(html);

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/paste", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result!.Markdown.Should().Contain("# Title");
        result.Markdown.Should().Contain("## Subtitle");
        result.Markdown.Should().Contain("### Section");
    }

    [Fact]
    public async Task ConvertPaste_ReturnsWarnings_WhenContentDetected()
    {
        // Arrange
        var html = "<main><h1>Content</h1></main>";
        var request = new ConvertRequest(html);

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/paste", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result!.Warnings.Should().NotBeEmpty();
        result.Warnings.Should().Contain(w => w.Contains("auto-detected"));
    }

    // URL Conversion Tests

    [Fact]
    public async Task ConvertUrl_WithValidUrl_ReturnsMarkdown()
    {
        // Arrange
        var request = new { url = "https://example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConvertResponse>();
        result.Should().NotBeNull();
        result!.Markdown.Should().Contain("Example Domain");
    }

    [Fact]
    public async Task ConvertUrl_WithEmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new { url = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertUrl_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new { url = "not-a-valid-url" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertUrl_WithLocalhostUrl_ReturnsForbidden()
    {
        // Arrange
        var request = new { url = "http://localhost/test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConvertUrl_WithPrivateIpAddress_ReturnsForbidden()
    {
        // Arrange
        var request = new { url = "http://192.168.1.1" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConvertUrl_WithLoopbackIp_ReturnsForbidden()
    {
        // Arrange
        var request = new { url = "http://127.0.0.1" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConvertUrl_WithUnsupportedProtocol_ReturnsBadRequest()
    {
        // Arrange
        var request = new { url = "ftp://example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/convert/url", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
