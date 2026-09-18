# Headline filtering work log

1. Inspected discovery, extraction and existing LLM rewriting. Found discovery
   retained only URLs, making it the earliest useful point for reducing downloads.
2. Created `codex/headline-filtering`. Preserved pre-existing edits in
   CrawlerOptions, LlmArticleAnalyzer, Program and appsettings.
3. Chose a separate configurable local filter instead of model-based selection:
   deterministic decisions, no extra tokens, and no article download for rejected
   listing headlines. Sensitive-topic rules take priority over positive hints.
4. Retained all available labels per URL and merged duplicates before filtering;
   applied the run limit afterward to preserve capacity for eligible articles.
5. Added a source-headline check before body extraction to handle changed titles
   and redirect destinations. Existing LLM rewriting remains downstream.
6. Documented methods introduced/modified and explained Unicode boundaries,
   wildcard matching and ordering decisions inline.
7. Added offline tests for keywords, entities/case, word boundaries, exclusions,
   configuration, duplicate labels, missing headlines, HTTP request suppression,
   limits and source-headline revalidation. No live crawling or publishing used.

Known tradeoff: keyword rules intentionally favor conservative exclusion and do
not understand context. Editorial tuning is needed for figurative tragedy and
sensitive topics expressed using vocabulary outside the configured list.

Validation result: `dotnet test WebCrawler.Tests/WebCrawler.Tests.csproj` passed
all 11 tests (0 failures). Crawler and test projects compiled successfully.
