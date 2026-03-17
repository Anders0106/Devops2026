using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Chirp.Razor.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;
    private readonly TimeSpan _slowThreshold;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger, int slowThresholdMs = 500)
    {
        _next = next;
        _logger = logger;
        _slowThreshold = TimeSpan.FromMilliseconds(slowThresholdMs);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context); 

        stopwatch.Stop();

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (stopwatch.Elapsed > _slowThreshold)
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} took {ElapsedMilliseconds} ms",
                ipAddress,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
        else 
        {
            _logger.LogInformation(
                "Request completed: {Method} {Path} took {ElapsedMilliseconds} ms",
                ipAddress,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}