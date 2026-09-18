namespace WebCrawler;

public sealed class CrawlerOptions
{
    public const string SectionName = "Crawler";

    public Uri ApiBaseUrl { get; set; } = new("http://localhost:5010");
    public string ApiKey { get; set; } = "super-duper-key";
    public Guid? AuthorId { get; set; }
    public string? AuthorEmail { get; set; } = "ana.trajkova@sokantno.test";
    public string DefaultCategoryId { get; set; } = "Свет";
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(2);
    public bool RunOnStartup { get; set; } = true;
    public bool DryRun { get; set; }
    public int MaxDiscoveryPagesPerRun { get; set; } = 25;
    public int MaxArticlesPerRun { get; set; } = 20;
    public int MinArticleBodyCharacters { get; set; } = 300;
    public int RequestDelayMilliseconds { get; set; } = 1_000;
    public int ApiStartupTimeoutSeconds { get; set; } = 120;
    public int ApiStartupRetryDelaySeconds { get; set; } = 2;
    public string HistoryFilePath { get; set; } = "crawler-history.json";
    public List<string> AllowedCategoryIds { get; set; } =
    [
        "Политика",
        "Технологија",
        "Спорт",
        "Свет",
        "Култура"
    ];

    public HeadlineFilterOptions HeadlineFilter { get; set; } = new();

    public LlmAnalysisOptions Llm { get; set; } = new();
    public List<string> SeedUrls { get; set; } =
    [
        "https://tocka.com.mk/"

    ];

    public TimeSpan EffectiveInterval =>
        Interval <= TimeSpan.Zero ? TimeSpan.FromHours(2) : Interval;

    public TimeSpan EffectiveApiStartupTimeout =>
        TimeSpan.FromSeconds(Math.Max(1, ApiStartupTimeoutSeconds));

    public TimeSpan EffectiveApiStartupRetryDelay =>
        TimeSpan.FromSeconds(Math.Max(1, ApiStartupRetryDelaySeconds));
}

public sealed class LlmAnalysisOptions
{
    public bool Enabled { get; set; }
    public Uri ApiBaseUrl { get; set; } = new("https://api.openai.com/v1/");
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxInputCharacters { get; set; } = 6_000;
    public int TimeoutSeconds { get; set; } = 45;
    public string Provider { get; set; } = "OpenAI";
}
