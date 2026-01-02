using UnWeb.Models;

namespace UnWeb.Services;

public interface IConversionService
{
    Task<ConvertResponse> ConvertHtmlToMarkdownAsync(string html);
    Task<ConvertResponse> ConvertUrlToMarkdownAsync(string url);
}
