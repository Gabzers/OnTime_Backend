using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnTime.Application.Common;
using OnTime.Application.DTOs.Brands;
using OnTime.Application.DTOs.Companies;
using OnTime.Application.Interfaces;
using OnTime.Application.Common;

namespace OnTime.API.Controllers;

/// <summary>
/// Platform-admin panel for managing ALL companies and their brands across the whole SaaS —
/// deliberately NOT manager-accessible: a Manager is a customer, not the operator, and this
/// panel can list, disable, or edit any other company's data.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IBrandService _brands;

    public AdminController(IAdminService admin, IBrandService brands)
    {
        _admin  = admin;
        _brands = brands;
    }

    // ── Companies ─────────────────────────────────────────────────────────

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        var result = await _admin.GetCompaniesAsync(ct);
        return Ok(result);
    }

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CreateCompanyAdminRequest request, CancellationToken ct)
    {
        var result = await _admin.CreateCompanyAsync(request, ct);
        return Created($"api/admin/companies/{result.Id}", result);
    }

    [HttpPut("companies/{id:guid}")]
    public async Task<IActionResult> UpdateCompany(
        Guid id, [FromBody] UpdateCompanyAdminRequest request, CancellationToken ct)
    {
        var result = await _admin.UpdateCompanyAsync(id, request, ct);
        return Ok(result);
    }

    [HttpPatch("companies/{id:guid}/active")]
    public async Task<IActionResult> SetCompanyActive(
        Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        await _admin.SetCompanyActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    // ── Brands (per company) ─────────────────────────────────────────────

    [HttpGet("companies/{companyId:guid}/brands")]
    public async Task<IActionResult> GetBrands(Guid companyId, CancellationToken ct)
    {
        var result = await _brands.GetByCompanyAsync(companyId, ct);
        return Ok(result);
    }

    [HttpPost("companies/{companyId:guid}/brands")]
    public async Task<IActionResult> CreateBrand(
        Guid companyId, [FromBody] CreateBrandRequest request, CancellationToken ct)
    {
        var result = await _brands.CreateAsync(companyId, request, ct);
        return Created($"api/admin/companies/{companyId}/brands/{result.Id}", result);
    }

    [HttpPut("companies/{companyId:guid}/brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(
        Guid companyId, Guid id, [FromBody] UpdateBrandRequest request, CancellationToken ct)
    {
        var result = await _brands.UpdateAsync(id, companyId, request, ct);
        return Ok(result);
    }

    [HttpPatch("companies/{companyId:guid}/brands/{id:guid}/active")]
    public async Task<IActionResult> SetBrandActive(
        Guid companyId, Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        await _brands.SetActiveAsync(id, companyId, request.IsActive, ct);
        return NoContent();
    }

    // ── Users (per company) ──────────────────────────────────────────────

    [HttpGet("companies/{companyId:guid}/users")]
    public async Task<IActionResult> GetUsers(Guid companyId, CancellationToken ct)
    {
        var result = await _admin.GetUsersByCompanyAsync(companyId, ct);
        return Ok(result);
    }

    [HttpPatch("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(
        Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        var result = await _admin.UpdateUserRoleAsync(id, request.Role, User.GetUserId(), ct);
        return Ok(result);
    }

    // ── Memberships (any company — platform Admin) ──────────────────────────

    [HttpPost("users/{id:guid}/memberships")]
    public async Task<IActionResult> GrantMembership(
        Guid id, [FromBody] GrantBrandMembershipRequest request, CancellationToken ct)
    {
        await _admin.GrantMembershipAsync(id, request.BrandId, ct);
        return NoContent();
    }

    [HttpDelete("users/{id:guid}/memberships/{brandId:guid}")]
    public async Task<IActionResult> RevokeMembership(Guid id, Guid brandId, CancellationToken ct)
    {
        await _admin.RevokeMembershipAsync(id, brandId, ct);
        return NoContent();
    }
}

public record SetActiveRequest(bool IsActive);
public record UpdateUserRoleRequest(int Role);
public record GrantBrandMembershipRequest(Guid BrandId);
