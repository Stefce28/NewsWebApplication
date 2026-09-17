# WebCrawler

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
