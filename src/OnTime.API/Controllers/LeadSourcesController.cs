using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnTime.Application.Common;
using OnTime.Application.DTOs.LeadSources;
using OnTime.Application.Interfaces;

namespace OnTime.API.Controllers;

[ApiController]
[Route("api/lead-sources")]
[Authorize]
public class LeadSourcesController : ControllerBase
{
    private readonly ILeadSourceService _leadSources;

    public LeadSourcesController(ILeadSourceService leadSources) => _leadSources = leadSources;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _leadSources.GetByCompanyAsync(User.RequireCompanyId(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    public async Task<IActionResult> Create([FromBody] CreateLeadSourceRequest request, CancellationToken ct)
    {
        var result = await _leadSources.CreateAsync(User.RequireCompanyId(), request, ct);
        return Created($"api/lead-sources/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadSourceRequest request, CancellationToken ct)
    {
        var result = await _leadSources.UpdateAsync(id, User.RequireCompanyId(), request, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = "ManagerOnly")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetLeadSourceActiveRequest request, CancellationToken ct)
    {
        await _leadSources.SetActiveAsync(id, User.RequireCompanyId(), request.IsActive, ct);
        return NoContent();
    }
}
