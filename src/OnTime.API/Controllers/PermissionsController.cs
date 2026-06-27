using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnTime.Application.DTOs.Permissions;
using OnTime.Application.Interfaces;

namespace OnTime.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "ManagerOnly")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissions;

    public PermissionsController(IPermissionService permissions) => _permissions = permissions;

    [HttpGet]
    public async Task<IActionResult> GetPermissions([FromQuery] int role, CancellationToken ct)
    {
        var result = await _permissions.GetPermissionsAsync(role, ct);
        return Ok(result);
    }

    [HttpPut("{role:int}")]
    public async Task<IActionResult> UpdatePermissions(
        int role,
        [FromBody] IEnumerable<UpdateMenuPermissionRequest> updates,
        CancellationToken ct)
    {
        await _permissions.UpdatePermissionsAsync(role, updates, ct);
        return NoContent();
    }
}
