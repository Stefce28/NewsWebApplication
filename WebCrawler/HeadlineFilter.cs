using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebCrawler;

public enum HeadlineDecision { Accepted, MissingHeadline, SensitiveTopic, NoComedyKeyword }

/// <summary>Local editorial rules; no article downloads or model calls are needed.</summary>
public sealed class HeadlineFilterOptions
{
    public bool Enabled { get; set; } = true;
    public string[] ComedyKeywords { get; set; } =
    [
        "шокантно", "неверојатно", "изненадувачки", "невозможно", "невидено",
        "смешно", "хумористично", "фрапантно", "запрепастувачко", "сензационално",
        "нечуено", "несекојдневно", "бизарно", "урнебесно", "апсурдно",
        "драматично", "скандалозно", "зачудувачко", "неочекувано"
    ];

    // A trailing * explicitly allows Macedonian inflections, rather than arbitrary substrings.
    public string[] SensitiveKeywords { get; set; } =
    [
        "загина*", "загинат*", "убиен*", "убиств*", "мртв*", "смрт*", "почина*",
        "погина*", "самоуби*", "масакр*", "терор*", "силува*", "повреден*",
        "ранет*", "жртв*", "трагеди*", "војна*", "војни*", "воени*", "бомбарди*",
        "фронт*", "репрес*", "тортур*", "протест*", "слобода на говор*",
        "слобода на изразување*", "човекови права*"
    ];
}

/// <summary>Requires a comedy hint and gives sensitive-topic exclusions precedence.</summary>
public sealed class HeadlineFilter
{
    private readonly HeadlineFilterOptions _options;
    private readonly Regex[] _comedy;
    private readonly Regex[] _sensitive;

    /// <summary>Compiles bounded, escaped keyword patterns once per crawl.</summary>
    public HeadlineFilter(HeadlineFilterOptions options)
    {
        _options = options;
        _comedy = Compile(options.ComedyKeywords);
        _sensitive = Compile(options.SensitiveKeywords);
    }

    /// <summary>Evaluates all labels for one URL, including duplicate links across listing pages.</summary>
    public HeadlineDecision Evaluate(IEnumerable<string> headlines)
    {
        if (!_options.Enabled) return HeadlineDecision.Accepted;
        var labels = headlines.Select(Normalize).Where(label => label.Length > 0).ToArray();
        if (labels.Length == 0) return HeadlineDecision.MissingHeadline;
        if (labels.Any(label => _sensitive.Any(pattern => pattern.IsMatch(label))))
            return HeadlineDecision.SensitiveTopic;
        return labels.Any(label => _comedy.Any(pattern => pattern.IsMatch(label)))
            ? HeadlineDecision.Accepted : HeadlineDecision.NoComedyKeyword;
    }

    /// <summary>Decodes listing markup and normalizes Unicode, casing and whitespace.</summary>
    private static string Normalize(string text) => Regex.Replace(
        HtmlEntity.DeEntitize(text).Normalize(NormalizationForm.FormC).ToLowerInvariant(),
        @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();

    /// <summary>Builds whole-word/phrase matchers with optional trailing prefix wildcards.</summary>
    private static Regex[] Compile(IEnumerable<string> keywords) => keywords
        .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
        .Select(Normalize)
        .Where(keyword => keyword.TrimEnd('*').Length > 0)
        .Select(keyword => new Regex(
            // Unicode boundaries avoid matching a keyword inside an unrelated Cyrillic word.
            @"(?<![\p{L}\p{N}_])" + Regex.Escape(keyword.TrimEnd('*')) +
            (keyword.EndsWith('*') ? @"[\p{L}\p{M}]*" : "") + @"(?![\p{L}\p{N}_])",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        .ToArray();
}
