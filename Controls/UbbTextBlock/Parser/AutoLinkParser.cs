using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace UbbRender.Parser;

/// <summary>
/// 自动链接识别器，用于在文本中识别 URL 并转换为 Url 节点
/// </summary>
public class AutoLinkParser
{
    // URL 匹配正则表达式
    // 支持 http://, https://, ftp://, file:// 以及 www.
    private static readonly Regex UrlRegex = new Regex(
        @"(?<url>(?:https?|ftp|file)://[^\s<>""'（）\[\]]+|www\.[^\s<>""'（）\[\]]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 解析文本，将其中识别的链接转换为 Url 节点
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>节点列表，包含 TextNode 和 UrlNode</returns>
    public static IEnumerable<UbbNode> Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        var matches = UrlRegex.Matches(text);

        // 如果没有匹配到链接，直接返回文本节点
        if (matches.Count == 0)
        {
            yield return new TextNode(text);
            yield break;
        }

        int lastIndex = 0;

        foreach (Match match in matches)
        {
            // 添加匹配前的文本
            if (match.Index > lastIndex)
            {
                string beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(beforeText))
                    yield return new TextNode(beforeText);
            }

            // 添加链接节点
            string url = match.Groups["url"].Value;
            yield return CreateUrlNode(url);

            lastIndex = match.Index + match.Length;
        }

        // 添加剩余的文本
        if (lastIndex < text.Length)
        {
            string remainingText = text.Substring(lastIndex);
            if (!string.IsNullOrEmpty(remainingText))
                yield return new TextNode(remainingText);
        }
    }

    /// <summary>
    /// 创建 Url 节点
    /// </summary>
    private static TagNode CreateUrlNode(string url)
    {
        var attributes = new Dictionary<string, string>
        {
            ["href"] = NormalizeUrl(url)
        };

        // 为 www 开头的链接添加协议
        string displayUrl = url.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? "http://" + url
            : url;

        // 创建 Url 节点，并添加显示文本作为子节点
        var urlNode = TagNode.Create(UbbNodeType.Url, attributes);
        urlNode.AddChild(new TextNode(url));

        return urlNode;
    }

    /// <summary>
    /// 规范化 URL，确保有协议前缀
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            return "http://" + url;

        return url;
    }

    /// <summary>
    /// 检查文本中是否包含链接（用于快速判断）
    /// </summary>
    public static bool ContainsAutoLink(string text)
    {
        return !string.IsNullOrEmpty(text) && UrlRegex.IsMatch(text);
    }
}