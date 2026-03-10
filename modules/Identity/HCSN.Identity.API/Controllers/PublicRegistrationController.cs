using System.Collections.Generic;
using System.Threading.Tasks;
using HCSN.Identity.Public;
using Microsoft.AspNetCore.Mvc;

namespace HCSN.Identity.API.Controllers;

[ApiController]
[Route("api/public/register")]
public class PublicRegistrationController : ControllerBase
{
    private readonly ITenantRegistrationService _registrationService;

    public PublicRegistrationController(ITenantRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet("settings/{subdomain}")]
    public async Task<ActionResult<RegistrationSettingsDto>> GetSettings(string subdomain)
    {
        var settings = await _registrationService.GetRegistrationSettingsAsync(subdomain);
        return Ok(settings);
    }

    [HttpGet("required-fields/{subdomain}")]
    public async Task<ActionResult<List<string>>> GetRequiredFields(string subdomain)
    {
        var fields = await _registrationService.GetRequiredFieldsAsync(subdomain);
        return Ok(fields);
    }

    [HttpPost("tenant")]
    public async Task<ActionResult<RegistrationResult>> RegisterForTenant(
        TenantRegisterRequest request
    )
    {
        var result = await _registrationService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("invite/{token}")]
    public async Task<ActionResult<RegistrationResult>> RegisterWithInvite(
        string token,
        BaseRegisterRequest request
    )
    {
        var result = await _registrationService.RegisterWithInviteAsync(token, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
