using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnTime.Application.Interfaces;

namespace OnTime.API.Controllers;

/// <summary>
/// Platform-admin read access to every error the API has returned (see
/// ErrorHandlingMiddleware, which persists one ErrorLog row per error). AdminOnly for the
/// same reason as AdminController: this is cross-tenant data, not something a customer's
/// Manager should be able to browse.
/// </summary>
[ApiController]
[Route("api/admin/error-logs")]
[Authorize(Policy = "AdminOnly")]
public class ErrorLogsController : ControllerBase
{
    private readonly IErrorLogService _errorLogs;

    public ErrorLogsController(IErrorLogService errorLogs) => _errorLogs = errorLogs;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? statusCode = null,
        CancellationToken ct = default)
    {
        var result = await _errorLogs.GetPagedAsync(page, pageSize, statusCode, ct);
        return Ok(result);
    }
}
