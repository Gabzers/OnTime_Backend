using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnTime.Application.Common;
using OnTime.Application.DTOs.Vehicles;
using OnTime.Application.Interfaces;

namespace OnTime.API.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicles;

    public VehiclesController(IVehicleService vehicles) => _vehicles = vehicles;

    // ── Brands (global catalogue — the "shelf" everyone picks from) ────────

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands(CancellationToken ct)
    {
        var result = await _vehicles.GetBrandsAsync(ct);
        return Ok(result);
    }

    [HttpPost("brands")]
    [Authorize(Policy = "ManagerOnly")]
    public async Task<IActionResult> CreateBrand(
        [FromBody] CreateVehicleBrandRequest request,
        CancellationToken ct)
    {
        var result = await _vehicles.CreateBrandAsync(request, ct);
        return Created($"api/vehicles/brands/{result.Id}", result);
    }

    [HttpDelete("brands/{id:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken ct)
    {
        await _vehicles.DeleteBrandAsync(id, ct);
        return NoContent();
    }

    // ── Models (personal catalogue — owned by the calling user) ─────────────

    [HttpGet("models")]
    public async Task<IActionResult> GetModels(
        [FromQuery] VehicleSearchParams p,
        CancellationToken ct)
    {
        var result = await _vehicles.GetModelsAsync(p, User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpGet("models/{id:guid}")]
    public async Task<IActionResult> GetModelById(Guid id, CancellationToken ct)
    {
        var result = await _vehicles.GetModelByIdAsync(id, User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("models")]
    public async Task<IActionResult> CreateModel(
        [FromBody] CreateVehicleModelRequest request,
        CancellationToken ct)
    {
        var result = await _vehicles.CreateModelAsync(request, User.GetUserId(), ct);
        return Created($"api/vehicles/models/{result.Id}", result);
    }

    [HttpPut("models/{id:guid}")]
    public async Task<IActionResult> UpdateModel(
        Guid id,
        [FromBody] UpdateVehicleModelRequest request,
        CancellationToken ct)
    {
        var result = await _vehicles.UpdateModelAsync(id, User.GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPatch("models/{id:guid}/active")]
    public async Task<IActionResult> SetModelActive(
        Guid id,
        [FromBody] SetVehicleModelActiveRequest request,
        CancellationToken ct)
    {
        await _vehicles.SetModelActiveAsync(id, User.GetUserId(), request.IsActive, ct);
        return NoContent();
    }

    [HttpDelete("models/{id:guid}")]
    public async Task<IActionResult> DeleteModel(Guid id, CancellationToken ct)
    {
        await _vehicles.DeleteModelAsync(id, User.GetUserId(), ct);
        return NoContent();
    }

    // ── Versions (personal catalogue) ───────────────────────────────────────

    [HttpGet("models/{modelId:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid modelId, CancellationToken ct)
    {
        var result = await _vehicles.GetVersionsAsync(modelId, User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("models/{modelId:guid}/versions")]
    public async Task<IActionResult> CreateVersion(
        Guid modelId,
        [FromBody] CreateVehicleVersionRequest request,
        CancellationToken ct)
    {
        var result = await _vehicles.CreateVersionAsync(modelId, User.GetUserId(), request, ct);
        return Created($"api/vehicles/models/{modelId}/versions/{result.Id}", result);
    }

    [HttpPut("models/{modelId:guid}/versions/{id:guid}")]
    public async Task<IActionResult> UpdateVersion(
        Guid modelId, Guid id,
        [FromBody] UpdateVehicleVersionRequest request,
        CancellationToken ct)
    {
        var result = await _vehicles.UpdateVersionAsync(id, User.GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpDelete("models/{modelId:guid}/versions/{id:guid}")]
    public async Task<IActionResult> DeleteVersion(Guid modelId, Guid id, CancellationToken ct)
    {
        await _vehicles.DeleteVersionAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
