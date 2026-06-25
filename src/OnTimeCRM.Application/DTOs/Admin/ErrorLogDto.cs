namespace OnTimeCRM.Application.DTOs.Admin;

public record ErrorLogDto(
    Guid Id,
    int StatusCode,
    string ErrorCode,
    string Message,
    string? Details,
    string Path,
    string Method,
    string TraceId,
    Guid? UserId,
    DateTimeOffset CreatedAt
);
