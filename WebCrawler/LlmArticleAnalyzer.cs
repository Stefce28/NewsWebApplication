using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebCrawler;

public sealed class LlmArticleAnalyzer : IArticleAnalyzer
{
    public const string HttpClientName = "LlmArticleAnalysis";

    private const string OllamaProvider = "Ollama";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<CrawlerOptions> _options;
    private readonly ILogger<LlmArticleAnalyzer> _logger;

    public LlmArticleAnalyzer(
        IHttpClientFactory httpClientFactory,
        IOptions<CrawlerOptions> options,
        ILogger<LlmArticleAnalyzer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<CrawledArticle> AnalyzeAsync(
        CrawledArticle article,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var llmOptions = options.Llm;
        var provider = OllamaProvider;

        if (!llmOptions.Enabled)
        {
            return article;
        }

        if (IsOllamaCloud(llmOptions, provider) &&
        string.IsNullOrWhiteSpace(llmOptions.ApiKey))
        {
            _logger.LogWarning(
                "Ollama Cloud analysis is enabled, but no Ollama API key is configured. " +
                "Using extracted default article values.");

            return article;
        }

        var allowedCategories = options.AllowedCategoryIds
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Select(category => category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedCategories.Length == 0)
        {
            _logger.LogWarning("No allowed categories are configured for LLM analysis. Using extracted default article values.");
            return article;
        }

        try
        {
            var request = CreateRequest(article, llmOptions, allowedCategories, provider);
            var endpoint = GetEndpoint(llmOptions, provider);
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "LLM analysis failed for '{Headline}'. {Provider} returned {StatusCode}: {Error}",
                    article.HeadLine,
                    provider,
                    response.StatusCode,
                    error);

                return article;
            }

            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var content = ExtractMessageContent(rawResponse, provider);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("LLM analysis returned no content for '{Headline}'. Using extracted default article values.", article.HeadLine);
                return article;
            }

            var jsonContent = ExtractJsonObject(content);
            var analysis = JsonSerializer.Deserialize<ArticleAnalysisResponse>(jsonContent, JsonOptions);
            if (analysis == null ||
                !allowedCategories.Contains(analysis.CategoryId, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("LLM analysis returned an invalid category for '{Headline}'. Using extracted default article values.", article.HeadLine);
                return article;
            }

            var categoryId = allowedCategories.First(category =>
                category.Equals(analysis.CategoryId, StringComparison.OrdinalIgnoreCase));
            var shockValue = Math.Clamp(Math.Round(analysis.ShockValue, 1), 1.0m, 10.0m);

            _logger.LogInformation(
                "LLM analyzed '{Headline}' with {Provider} model '{Model}' as category '{CategoryId}' with shock value {ShockValue}. Reason: {Reason}",
                article.HeadLine,
                provider,
                llmOptions.Model,
                categoryId,
                shockValue,
                analysis.Reason);

            return article with
            {
                HeadLine = analysis.Headline,
                Body = analysis.Body,
                CategoryId = categoryId,
                ShockValue = shockValue
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "LLM analysis failed for '{Headline}'. Using extracted default article values.", article.HeadLine);
            return article;
        }
    }

    

    private static string GetEndpoint(LlmAnalysisOptions llmOptions, string provider)
    {
        if (provider != OllamaProvider)
        {
            return "chat/completions";
        }

        var basePath = llmOptions.ApiBaseUrl.AbsolutePath.TrimEnd('/');
        return basePath.EndsWith("/api", StringComparison.OrdinalIgnoreCase)
            ? "chat"
            : "api/chat";
    }

    private static object CreateRequest(
        CrawledArticle article,
        LlmAnalysisOptions llmOptions,
        IReadOnlyList<string> allowedCategories,
        string provider)
    {
        var analysisSchema = CreateAnalysisSchema(allowedCategories);
        var isOllamaCloud = IsOllamaCloud(llmOptions, provider);
        var messages = CreateMessages(article, llmOptions, allowedCategories, analysisSchema, isOllamaCloud);

        if (provider == OllamaProvider)
        {
            if (isOllamaCloud)
            {
                return new
                {
                    model = llmOptions.Model,
                    messages,
                    stream = false,
                    options = new
                    {
                        temperature = 0.1
                    }
                };
            }

            return new
            {
                model = llmOptions.Model,
                messages,
                stream = false,
                format = analysisSchema,
                options = new
                {
                    temperature = 0.1
                }
            };
        }

        return new
        {
            model = llmOptions.Model,
            messages,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "article_analysis",
                    strict = true,
                    schema = analysisSchema
                }
            },
            temperature = 0.1
        };
    }

    private static object[] CreateMessages(
        CrawledArticle article,
        LlmAnalysisOptions llmOptions,
        IReadOnlyList<string> allowedCategories,
        object analysisSchema,
        bool isOllamaCloud)
    {
        var body = article.Body.Length <= llmOptions.MaxInputCharacters
            ? article.Body
            : article.Body[..llmOptions.MaxInputCharacters];
        var schemaJson = JsonSerializer.Serialize(analysisSchema);

        return
        [
            new
            {
                role = "system",
                content = string.Join(
                    Environment.NewLine,
                     "You analyze Macedonian news articles for a news application.",
                "Return exactly one valid JSON object and no markdown.",
                "Turn the headline and content/body of the article into a comedic, satirical, exaggerated or intentionally unrealistic version that people would find funny, surprising or shocking, while still keeping a recognizable connection to the original article.",
                "You may add humorous fictional elements, references to current trends, comparisons with other events, Macedonian estrada, celebrities, politics, sports, internet culture or other relevant topics when they make the rewritten article funnier or more surprising.",                                       
                "Do not completely replace the original topic with an unrelated story.",
                "Choose exactly one category from the provided enum.",
                "Assign a shock value from 1.0 to 10.0.",
                "Use the full shock-value scale. Do not automatically assign a very high value simply because the rewritten article is satirical or exaggerated.",
                "Use the following scoring guide:",
                "1.0-2.0 = Very normal, predictable, barely surprising or barely funny.",
                "2.1-4.0 = Mildly unusual, amusing or somewhat unexpected.",
                "4.1-6.0 = Clearly funny, surprising or noticeably exaggerated.",
                "6.1-8.0 = Strongly surprising, absurd, comedic or unrealistic.",
                "8.1-9.0 = Extremely unusual, highly shocking, very absurd or exceptionally funny.",
                "9.1-10.0 = Reserve only for exceptional cases that are extraordinarily absurd, shocking, unexpected or memorable.",
                "Scores above 9.0 should be uncommon.",
                "A typical satirical article should usually fall somewhere between 4.0 and 8.0 unless there is a strong reason to score it higher.",
                "Do not repeatedly use the same shock value for different articles.",
                "Compare the article against the entire 1.0 to 10.0 scale before selecting the final value.",
                "The shock value should represent the intensity of the final rewritten article, including how surprising, funny, absurd, unrealistic or unexpected it is.",
                "Do not invent categories.",

                    isOllamaCloud
                        ? "Ollama Cloud does not enforce the schema for this request, so you must follow it exactly from the prompt."
                        : "Follow the provided JSON schema exactly.")
            },
            new
            {
                role = "user",
                content = string.Join(
                    Environment.NewLine,
                    $"Allowed categories: {string.Join(", ", allowedCategories)}",
                    "Required JSON schema:",
                    schemaJson,
                    $"Source URL: {article.SourceUrl}",
                    $"Headline: {article.HeadLine}",
                    "Article body:",
                    body
                )
            }
        ];
    }

    private static object CreateAnalysisSchema(IReadOnlyList<string> allowedCategories)
    {
        return new
        {
            type = "object",
            properties = new
            {
                categoryId = new
                {
                    type = "string",
                    description = "The chosen application category.",
                    @enum = allowedCategories
                },
                shockValue = new
                {
                    type = "number",
                    description = "Decimal shock score from 1.0 to 10.0."
                },
                reason = new
                {
                    type = "string",
                    description = "Short explanation for the chosen category and shock value."
                },
                body = new
                {
                    type = "string",
                    description = "The comedic/satiric or totally unreal version of the article content."
                },
                headline = new
                {
                    type = "string",
                    description = "The comedic/satiric or totally unreal version of the article headline."
                }
            },
            required = new[] { "categoryId", "shockValue", "reason", "body", "headline" },
            additionalProperties = false
        };
    }

    private static string? ExtractMessageContent(string rawResponse, string provider)
    {
        using var document = JsonDocument.Parse(rawResponse);

        if (provider == OllamaProvider)
        {
            if (!document.RootElement.TryGetProperty("message", out var ollamaMessage) ||
                !ollamaMessage.TryGetProperty("content", out var ollamaContent))
            {
                return null;
            }

            return ollamaContent.GetString();
        }

        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.GetString();
    }

    private static bool IsOllamaCloud(LlmAnalysisOptions llmOptions, string provider)
    {
        return provider == OllamaProvider &&
            llmOptions.ApiBaseUrl.Host.Equals("ollama.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        var objectStart = trimmed.IndexOf('{');
        var objectEnd = trimmed.LastIndexOf('}');

        if (objectStart >= 0 && objectEnd > objectStart)
        {
            return trimmed[objectStart..(objectEnd + 1)];
        }

        return trimmed;
    }

    private sealed class ArticleAnalysisResponse
    {
        public string CategoryId { get; set; } = string.Empty;
        public decimal ShockValue { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
    }
}
