using OnTimeCRM.Application.Common;
using OnTimeCRM.Application.DTOs.Admin;

namespace OnTimeCRM.Application.Interfaces;

public interface IErrorLogService
{
    Task<PagedResult<ErrorLogDto>> GetPagedAsync(
        int page, int pageSize, int? statusCode = null, CancellationToken ct = default);
}
