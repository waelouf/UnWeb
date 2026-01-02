using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using UnWeb.Services;
using Xunit;

namespace UnWeb.Tests.Services;

public class ConversionServiceTests
{
    private readonly ConversionService _sut;
    private readonly Mock<ILogger<ConversionService>> _loggerMock;
    private readonly HttpClient _httpClient;

    public ConversionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConversionService>>();
        _httpClient = new HttpClient();
        _sut = new ConversionService(_loggerMock.Object, _httpClient);
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_SimpleHtml_ReturnsMarkdown()
    {
        // Arrange
        var html = "<h1>Hello World</h1><p>This is a test.</p>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Should().NotBeNull();
        result.Markdown.Should().Contain("# Hello World");
        result.Markdown.Should().Contain("This is a test.");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_HtmlWithMain_ExtractsMainContent()
    {
        // Arrange
        var html = @"
            <html>
                <head><title>Test</title></head>
                <body>
                    <nav><a href='/'>Home</a></nav>
                    <main>
                        <h1>Main Content</h1>
                        <p>This should be extracted.</p>
                    </main>
                    <footer>Copyright 2025</footer>
                </body>
            </html>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("# Main Content");
        result.Markdown.Should().Contain("This should be extracted.");
        result.Markdown.Should().NotContain("Home");
        result.Markdown.Should().NotContain("Copyright");
        result.Warnings.Should().BeEmpty();
	}

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_HtmlWithArticle_ExtractsArticleContent()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <nav>Navigation</nav>
                    <article>
                        <h1>Article Title</h1>
                        <p>Article content here.</p>
                    </article>
                    <aside>Sidebar content</aside>
                </body>
            </html>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("# Article Title");
        result.Markdown.Should().Contain("Article content here.");
        result.Markdown.Should().NotContain("Navigation");
        result.Markdown.Should().NotContain("Sidebar");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_Lists_ConvertsCorrectly()
    {
        // Arrange
        var html = @"
            <main>
                <h2>Ordered List</h2>
                <ol>
                    <li>First item</li>
                    <li>Second item</li>
                    <li>Third item</li>
                </ol>
                <h2>Unordered List</h2>
                <ul>
                    <li>Item A</li>
                    <li>Item B</li>
                </ul>
            </main>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("1.");
        result.Markdown.Should().Contain("First item");
        result.Markdown.Should().Contain("Second item");
        result.Markdown.Should().Contain("Item A");
        result.Markdown.Should().Contain("Item B");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_LinksAndImages_ConvertsCorrectly()
    {
        // Arrange
        var html = @"
            <main>
                <p>Check out <a href='https://example.com'>this link</a>.</p>
                <img src='image.jpg' alt='Test Image' />
            </main>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("[this link](https://example.com)");
        result.Markdown.Should().Contain("![Test Image](image.jpg)");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_Emphasis_ConvertsCorrectly()
    {
        // Arrange
        var html = @"
            <main>
                <p>This is <strong>bold</strong> and this is <em>italic</em>.</p>
            </main>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("**bold**");
        result.Markdown.Should().Contain("*italic*");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_MultipleHeadingLevels_ConvertsCorrectly()
    {
        // Arrange
        var html = @"
            <main>
                <h1>H1 Title</h1>
                <h2>H2 Subtitle</h2>
                <h3>H3 Section</h3>
                <h4>H4 Subsection</h4>
            </main>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("# H1 Title");
        result.Markdown.Should().Contain("## H2 Subtitle");
        result.Markdown.Should().Contain("### H3 Section");
        result.Markdown.Should().Contain("#### H4 Subsection");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_EmptyHtml_ReturnsEmptyMarkdown()
    {
        // Arrange
        var html = "<html><body></body></html>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().NotBeNull();
        result.Markdown.Trim().Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_NoSemanticTags_FallsBackToBody()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div>
                        <h1>Content in div</h1>
                        <p>Some content here.</p>
                    </div>
                </body>
            </html>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("# Content in div");
        result.Markdown.Should().Contain("Some content here.");
        result.Warnings.Should().BeEmpty();
	}

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_ComplexNestedStructure_ConvertsCorrectly()
    {
        // Arrange
        var html = @"
            <main>
                <h1>Main Title</h1>
                <div>
                    <h2>Section 1</h2>
                    <p>Paragraph with <strong>bold</strong> and <em>italic</em> text.</p>
                    <ul>
                        <li>List item 1</li>
                        <li>List item 2 with <a href='#'>link</a></li>
                    </ul>
                </div>
            </main>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("# Main Title");
        result.Markdown.Should().Contain("## Section 1");
        result.Markdown.Should().Contain("**bold**");
        result.Markdown.Should().Contain("*italic*");
        result.Markdown.Should().Contain("List item 1");
    }

    [Fact]
    public async Task ConvertHtmlToMarkdownAsync_FiltersOutScriptsAndStyles()
    {
        // Arrange
        var html = @"
            <html>
                <head>
                    <style>body { color: red; }</style>
                    <script>console.log('test');</script>
                </head>
                <body>
                    <main>
                        <h1>Content</h1>
                        <p>Visible content</p>
                    </main>
                    <script>alert('popup');</script>
                </body>
            </html>";

        // Act
        var result = await _sut.ConvertHtmlToMarkdownAsync(html);

        // Assert
        result.Markdown.Should().Contain("# Content");
        result.Markdown.Should().Contain("Visible content");
        result.Markdown.Should().NotContain("console.log");
        result.Markdown.Should().NotContain("alert");
        result.Markdown.Should().NotContain("color: red");
    }
}
