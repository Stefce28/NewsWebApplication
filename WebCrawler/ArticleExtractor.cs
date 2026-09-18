using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;

namespace WebCrawler;

public sealed partial class ArticleExtractor
{
    private static readonly string[] ContentXPaths =
    [
        "//*[@itemprop='articleBody']",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' entry-content ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' post-content ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' article-content ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' single-content ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' elementor-widget-theme-post-content ')]",
        "//article",
        "//main"
    ];

    private static readonly string[] CleanupXPaths =
    [
        "//script",
        "//style",
        "//noscript",
        "//iframe",
        "//svg",
        "//nav",
        "//footer",
        "//header",
        "//form",
        "//aside",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' sharedaddy ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' share ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' related ')]",
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' comments ')]"
    ];

    private readonly IOptions<CrawlerOptions> _options;

    public ArticleExtractor(IOptions<CrawlerOptions> options)
    {
        _options = options;
    }

    /// <summary>Validates the source headline before extracting the body and category.</summary>
    public CrawledArticle? Extract(FetchedPage page)
    {
        var document = new HtmlDocument();
        document.LoadHtml(page.Html);
        RemoveNoise(document);

        var headline = CleanHeadline(FirstNonEmpty(
            GetMeta(document, "property", "og:title"),
            GetMeta(document, "name", "twitter:title"),
            GetNodeText(document, "//h1[contains(concat(' ', normalize-space(@class), ' '), ' entry-title ')]"),
            GetNodeText(document, "//h1")));

        // The source title may differ from the listing label or redirect target.
        if (string.IsNullOrWhiteSpace(headline) ||
            new HeadlineFilter(_options.Value.HeadlineFilter).Evaluate([headline]) != HeadlineDecision.Accepted)
        {
            return null;
        }

        var body = ExtractBody(document);
        if (body.Length < _options.Value.MinArticleBodyCharacters)
        {
            return null;
        }

        return new CrawledArticle(
            page.FinalUri.AbsoluteUri,
            headline,
            body,
            ResolveCategory(document, page.FinalUri, headline, body),
            0m);
    }

    private static string ExtractBody(HtmlDocument document)
    {
        var candidates = new List<string>();

        foreach (var xpath in ContentXPaths)
        {
            var nodes = document.DocumentNode.SelectNodes(xpath);
            if (nodes is null)
            {
                continue;
            }

            foreach (var node in nodes)
            {
                var paragraphs = node
                    .SelectNodes(".//p|.//li")
                    ?.Select(paragraph => NormalizeText(paragraph.InnerText))
                    .Where(text => text.Length >= 40)
                    .Where(text => !IsBoilerplate(text))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (paragraphs is { Count: > 0 })
                {
                    candidates.Add(string.Join(Environment.NewLine + Environment.NewLine, paragraphs));
                    continue;
                }

                var text = NormalizeText(node.InnerText);
                if (text.Length >= 250 && !IsBoilerplate(text))
                {
                    candidates.Add(text);
                }
            }
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(candidate => candidate.Length)
            .FirstOrDefault() ?? string.Empty;
    }

    private string ResolveCategory(HtmlDocument document, Uri sourceUri, string headline, string body)
    {
        var hints = string.Join(
            ' ',
            GetMeta(document, "property", "article:section"),
            GetMeta(document, "name", "category"),
            sourceUri.AbsolutePath.Replace('/', ' '),
            headline,
            body[..Math.Min(body.Length, 1_000)]);

        var normalizedHints = hints.ToLowerInvariant();

        if (ContainsAny(normalizedHints, "спорт", "фудбал", "кошарка", "ракомет", "тенис", "атлет"))
        {
            return "Спорт";
        }

        if (ContainsAny(normalizedHints, "технолог", "хај-тек", "хајтек", "вештачка интелигенција", "стартап", "дигитал", "кибер"))
        {
            return "Технологија";
        }

        if (ContainsAny(normalizedHints, "култура", "култ", "филм", "музика", "театар", "книга", "литератур", "излож", "сцена"))
        {
            return "Култура";
        }

        if (ContainsAny(normalizedHints, "политика", "влада", "собрание", "министер", "избор", "парти", "опозиција", "антикорупциска", "македонија", "скопје"))
        {
            return "Политика";
        }

        if (ContainsAny(normalizedHints, "свет", "глобал", "европа", "русија", "украина", "балкан", "трамп"))
        {
            return "Свет";
        }

        return _options.Value.DefaultCategoryId;
    }

    private static void RemoveNoise(HtmlDocument document)
    {
        foreach (var xpath in CleanupXPaths)
        {
            var nodes = document.DocumentNode.SelectNodes(xpath);
            if (nodes is null)
            {
                continue;
            }

            foreach (var node in nodes.ToList())
            {
                node.Remove();
            }
        }
    }

    private static string CleanHeadline(string? headline)
    {
        if (string.IsNullOrWhiteSpace(headline))
        {
            return string.Empty;
        }

        var cleaned = NormalizeText(headline)
            .Replace(" - Слободен печат", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" - Sloboden Pechat", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" - Trn.mk", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" | TIME.mk", string.Empty, StringComparison.OrdinalIgnoreCase);

        return cleaned.Trim();
    }

    private static string? GetMeta(HtmlDocument document, string attributeName, string attributeValue)
    {
        var node = document.DocumentNode.SelectSingleNode(
            $"//meta[translate(@{attributeName}, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') = '{attributeValue.ToLowerInvariant()}']");

        return node?.GetAttributeValue("content", null);
    }

    private static string? GetNodeText(HtmlDocument document, string xpath)
    {
        return document.DocumentNode.SelectSingleNode(xpath)?.InnerText;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static bool ContainsAny(string text, params string[] candidates)
    {
        return candidates.Any(candidate => text.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBoilerplate(string text)
    {
        return ContainsAny(
            text.ToLowerInvariant(),
            "read more",
            "прочитај повеќе",
            "слични вести",
            "најчитани вести",
            "маркетинг",
            "cookie",
            "колачи");
    }

    private static string NormalizeText(string value)
    {
        var decoded = HtmlEntity.DeEntitize(value).Replace('\u00a0', ' ');
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
