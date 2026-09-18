using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using WebCrawler;

var runOnce = args.Any(arg => arg.Equals("--run-once", StringComparison.OrdinalIgnoreCase));
var hostArgs = args
    .Where(arg => !arg.Equals("--run-once", StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = hostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.Configure<CrawlerOptions>(
    builder.Configuration.GetSection(CrawlerOptions.SectionName));

builder.Services.AddSingleton(new CrawlerRuntimeOptions(runOnce));
builder.Services.AddSingleton<CrawlHistoryStore>();
builder.Services.AddSingleton<ArticleExtractor>();
builder.Services.AddSingleton<NewsCrawler>();
builder.Services.AddSingleton<ArticlePublisher>();
builder.Services.AddSingleton<PassthroughArticleAnalyzer>();
builder.Services.AddSingleton<LlmArticleAnalyzer>();
builder.Services.AddSingleton<IArticleAnalyzer>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CrawlerOptions>>().Value;

    return options.Llm.Enabled
        ? serviceProvider.GetRequiredService<LlmArticleAnalyzer>()
        : serviceProvider.GetRequiredService<PassthroughArticleAnalyzer>();
});
builder.Services.AddHostedService<CrawlerWorker>();

builder.Services.AddHttpClient(NewsCrawler.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("NewsWebAppCrawler/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("mk-MK,mk;q=0.9,en;q=0.6");
});

builder.Services.AddHttpClient(ArticlePublisher.HttpClientName, (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CrawlerOptions>>().Value;

    client.BaseAddress = options.ApiBaseUrl;
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
});

builder.Services.AddHttpClient(LlmArticleAnalyzer.HttpClientName, (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CrawlerOptions>>().Value.Llm;

    client.BaseAddress = new Uri(options.ApiBaseUrl.ToString().TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));

    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
});

await builder.Build().RunAsync();
