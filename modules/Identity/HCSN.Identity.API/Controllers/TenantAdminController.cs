using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCSN.Identity.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCSN.Identity.API.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/tenants")]
public class TenantAdminController : ControllerBase
{
    private readonly ITenantAdminService _adminService;
    private readonly ITenantService _tenantService;

    public TenantAdminController(ITenantAdminService adminService, ITenantService tenantService)
    {
        _adminService = adminService;
        _tenantService = tenantService;
    }

    [HttpGet("pending-approvals")]
    public async Task<ActionResult<List<PendingUserDto>>> GetPendingApprovals()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return BadRequest("No tenant context");

        var pending = await _adminService.GetPendingApprovalsAsync(tenantId.Value);
        return Ok(pending);
    }

    [HttpPost("users/{userId}/approve")]
    public async Task<ActionResult<RegistrationResult>> ApproveUser(Guid userId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return BadRequest("No tenant context");

        var result = await _adminService.ApproveUserAsync(userId, tenantId.Value);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("users/{userId}/reject")]
    public async Task<ActionResult<RegistrationResult>> RejectUser(
        Guid userId,
        [FromBody] string reason
    )
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return BadRequest("No tenant context");

        var result = await _adminService.RejectUserAsync(userId, tenantId.Value, reason);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
