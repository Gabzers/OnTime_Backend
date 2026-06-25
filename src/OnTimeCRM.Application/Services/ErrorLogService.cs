using Microsoft.EntityFrameworkCore;
using OnTimeCRM.Application.Common;
using OnTimeCRM.Application.DTOs.Admin;
using OnTimeCRM.Application.Interfaces;

namespace OnTimeCRM.Application.Services;

public class ErrorLogService : IErrorLogService
{
    private readonly IAppDbContext _db;

    public ErrorLogService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<ErrorLogDto>> GetPagedAsync(
        int page, int pageSize, int? statusCode = null, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 100);
        var query = _db.ErrorLogs.AsNoTracking().AsQueryable();

        if (statusCode.HasValue)
            query = query.Where(e => e.StatusCode == statusCode.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new ErrorLogDto(
                e.Id, e.StatusCode, e.ErrorCode, e.Message, e.Details,
                e.Path, e.Method, e.TraceId, e.UserId, e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ErrorLogDto>(items, total, page, size);
    }
}
