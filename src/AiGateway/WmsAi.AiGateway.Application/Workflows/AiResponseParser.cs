using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WmsAi.AiGateway.Application.Workflows;

/// <summary>
/// AI 响应解析器 - 统一处理 AI 返回的 JSON 响应
/// </summary>
public static class AiResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// 解析 AI 响应为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="aiContent">AI 返回的原始内容</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>解析后的对象</returns>
    /// <exception cref="InvalidOperationException">解析失败时抛出</exception>
    public static T Parse<T>(string aiContent, ILogger logger) where T : class
    {
        try
        {
            var jsonContent = ExtractJson(aiContent);

            var result = JsonSerializer.Deserialize<T>(jsonContent, JsonOptions);

            if (result == null)
            {
                throw new InvalidOperationException($"AI 返回的 JSON 解析为 null");
            }

            return result;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "解析 AI 响应失败: {Content}", aiContent);
            throw new InvalidOperationException(
                $"无法解析 AI 响应为 {typeof(T).Name}: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// 从 AI 响应中提取 JSON 内容（处理 markdown 代码块）
    /// </summary>
    private static string ExtractJson(string aiContent)
    {
        var content = aiContent.Trim();

        // 处理 ```json ... ``` 格式
        if (content.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            return content
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();
        }

        // 处理 ``` ... ``` 格式
        if (content.StartsWith("```"))
        {
            return content.Replace("```", "").Trim();
        }

        return content;
    }
}
