using OnTime.Application.Common;
using OnTime.Application.DTOs.Admin;

namespace OnTime.Application.Interfaces;

public interface IErrorLogService
{
    Task<PagedResult<ErrorLogDto>> GetPagedAsync(
        int page, int pageSize, int? statusCode = null, CancellationToken ct = default);
}
