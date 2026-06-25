using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnTimeCRM.Application.Common;
using OnTimeCRM.Domain.Entities;
using OnTimeCRM.Infrastructure.Persistence;

namespace OnTimeCRM.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    // AppDbContext is scoped — resolved per-request as an Invoke parameter, not via the
    // singleton constructor, which only runs once at app startup.
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await WriteErrorAsync(context, db, ex.Error.StatusCode, ex.Error.Code,
                ex.Error.Message, ex.Error.Class, ex.Details);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await WriteErrorAsync(context, db, 409, "CONFLICT",
                "A record with these values already exists.", "Conflict", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, db, 500, "INTERNAL_ERROR",
                "An unexpected error occurred.", "InternalError", ex.ToString());
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // Check inner exception message for Postgres error code 23505
        return ex.InnerException?.Message?.Contains("23505") == true
            || ex.InnerException?.GetType().Name == "PostgresException"
               && (ex.InnerException.Message?.Contains("unique") == true
                   || ex.InnerException.Message?.Contains("23505") == true);
    }

    private async Task WriteErrorAsync(
        HttpContext context,
        AppDbContext db,
        int status,
        string code,
        string message,
        string errorClass,
        string? details)
    {
        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/json";

        var traceId = context.TraceIdentifier;

        await PersistErrorLogAsync(context, db, status, code, message, details, traceId);

        var body = JsonSerializer.Serialize(new
        {
            code,
            message,
            @class   = errorClass,
            details,
            traceId
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }

    // Best-effort — a failure to log an error must never mask or replace the error response
    // that's actually going back to the caller, so any exception here is swallowed (after
    // logging it through ILogger, which doesn't touch the DB).
    private async Task PersistErrorLogAsync(
        HttpContext context, AppDbContext db, int status, string code, string message,
        string? details, string traceId)
    {
        try
        {
            // A DbUpdateException leaves its failed entities tracked in this same scoped
            // context — saving again without clearing would just re-attempt (and re-fail)
            // them alongside the new log row.
            db.ChangeTracker.Clear();

            Guid? userId = Guid.TryParse(
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out var parsed) ? parsed : null;

            db.ErrorLogs.Add(new ErrorLog
            {
                StatusCode = status,
                ErrorCode  = code,
                Message    = Truncate(message, 1000)!,
                Details    = Truncate(details, 2000),
                Path       = Truncate(context.Request.Path.Value ?? string.Empty, 500)!,
                Method     = context.Request.Method,
                TraceId    = traceId,
                UserId     = userId,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist error log entry");
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
