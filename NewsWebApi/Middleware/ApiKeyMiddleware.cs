namespace NewsWebApi.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ApiKeyHeader = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
        HttpContext context,
        IConfiguration configuration)
        {
            if(context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }
            if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API key missing");
                return;
            }

            var actualKey = configuration["ApiKey"];

            if (providedKey != actualKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }

            await _next(context);
        }

    }
}
