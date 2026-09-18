# WebCrawler

## Headline filtering

Filtering is enabled by default under `Crawler:HeadlineFilter`. Listing pages are
still downloaded for discovery, but article pages are requested only when their
link text, title, accessible label or image alt text contains a configured comedy
keyword and none of their labels contains a sensitive-topic keyword. Duplicate
URLs are evaluated together, before the article limit and source balancing.
Missing or unmatched headlines are skipped without an article request or LLM call.
The extracted source headline is checked again before body extraction, since it
may differ from its listing label. Accepted articles continue through the existing
LLM rewrite and publishing pipeline.

`ComedyKeywords` defaults to the 19 Macedonian hints in `HeadlineFilterOptions`.
`SensitiveKeywords` excludes indicators of death, injury, war, violence and civil
rights/repression topics, even when a comedy hint is present. Matching ignores
case, normalizes Unicode/HTML entities and whitespace, and uses whole words or
phrases. A trailing `*` explicitly matches word endings (for example `загина*`).
Both arrays can be configured under `Crawler:HeadlineFilter`; with .NET indexed
configuration binding, shorter arrays can retain later default entries, so review
the effective list when overriding defaults. `Enabled=false` restores legacy
unfiltered crawling.

These are editorial heuristics, not a semantic safety classifier: figurative or
theatrical tragedies can be excluded and unlisted sensitive wording can be missed.
Ordinary funny stories without a configured hint are also skipped. Debug logs
record rejection reasons and information logs report accepted/discovered counts.
No additional model calls are made for filtering.

Offline checks: `dotnet test WebCrawler.Tests/WebCrawler.Tests.csproj`.

Runs a hosted news crawler that discovers Macedonian news articles, extracts article text, optionally asks an LLM to classify the article, and publishes new articles to the NewsWebApi.

## Schedule

The crawler runs on startup and then every two hours by default:

```powershell
dotnet run --project WebCrawler\WebCrawler.csproj
```

For a single manual run:

```powershell
dotnet run --project WebCrawler\WebCrawler.csproj -- --run-once
```

For a no-write test run:

```powershell
$env:Crawler__DryRun = "true"
dotnet run --project WebCrawler\WebCrawler.csproj -- --run-once
```

## API startup

When the whole solution starts from Visual Studio, the crawler may start before `NewsWebApi` is ready. The crawler waits up to two minutes for the API before crawling so it does not spend LLM credits on articles it cannot save.

```powershell
$env:Crawler__ApiStartupTimeoutSeconds = "120"
$env:Crawler__ApiStartupRetryDelaySeconds = "2"
```

## LLM analysis

LLM analysis is disabled by default. When enabled, the crawler sends the extracted headline and body to the configured LLM provider, receives structured JSON, validates the category against `Crawler:AllowedCategoryIds`, and clamps the shock value to `1.0` through `10.0`.

If LLM analysis is disabled or fails, the crawler publishes the extracted article with a neutral shock value of `0` instead of guessing a score.

Use environment variables for OpenAI secrets:

```powershell
$env:Crawler__Llm__Provider = "OpenAI"
$env:Crawler__Llm__Enabled = "true"
$env:Crawler__Llm__ApiKey = "<openai-api-key>"
$env:Crawler__Llm__Model = "gpt-4o-mini"
dotnet run --project WebCrawler\WebCrawler.csproj -- --run-once
```

For free local Ollama, install and start Ollama, pull a model, then use the local provider:

```powershell
ollama pull qwen2.5:7b

$env:Crawler__Llm__Provider = "Ollama"
$env:Crawler__Llm__Enabled = "true"
$env:Crawler__Llm__ApiBaseUrl = "http://localhost:11434/"
$env:Crawler__Llm__ApiKey = ""
$env:Crawler__Llm__Model = "qwen2.5:7b"
dotnet run --project WebCrawler\WebCrawler.csproj -- --run-once
```

For Ollama Cloud, use your Ollama API key and a cloud model returned by `https://ollama.com/api/tags`:

```powershell
$env:Crawler__Llm__Provider = "Ollama"
$env:Crawler__Llm__Enabled = "true"
$env:Crawler__Llm__ApiBaseUrl = "https://ollama.com/api/"
$env:Crawler__Llm__ApiKey = "<ollama-api-key>"
$env:Crawler__Llm__Model = "gemma4:31b"
dotnet run --project WebCrawler\WebCrawler.csproj -- --run-once
```

The app API settings can be overridden the same way:

```powershell
$env:Crawler__ApiBaseUrl = "http://localhost:5010"
$env:Crawler__ApiKey = "<news-api-key>"
```
