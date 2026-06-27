using OnTime.Domain.Common;

namespace OnTime.Domain.Entities;

/// <summary>Persisted record of every error response the API returned — written once, by
/// ErrorHandlingMiddleware, regardless of whether it came from an ApiException, a DB
/// conflict, or an unhandled exception. CreatedAt (from BaseEntity) is when it occurred.</summary>
public class ErrorLog : BaseEntity
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
}
