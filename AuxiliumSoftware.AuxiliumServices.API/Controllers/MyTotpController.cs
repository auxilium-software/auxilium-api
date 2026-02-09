using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.MyTotp;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[ApiController]
[Route("/api/v3/me/totp")]
[Tags("Account Management", "Multi-Factor Authentication")]
public class MyTotpController : LoggedInControllerBase
{
    private readonly ITotpService _totpService;

    public MyTotpController(
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        AuxiliumDbContext db,
        IWafService waf,
        ILogger<MyTotpController> logger,
        ITotpService totpService
    )
        : base(systemSettingsService, configuration, db, waf, logger, totpService)
    {
        _totpService = totpService;
    }


    [HttpGet("status")]
    [ProducesResponseType(typeof(TotpStatusResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TotpStatusResponseModel>> GetTotpStatus()
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var isEnabled = await _totpService.IsTotpEnabledAsync(user!.Id);

            return Ok(new TotpStatusResponseModel { IsEnabled = isEnabled });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check TOTP status");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to check TOTP status"
            });
        }
    }


    [HttpPost("setup")]
    [ProducesResponseType(typeof(TotpSetupResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TotpSetupResponseModel>> SetupTotp()
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            // stop setup attempt if totp is already enabled
            if (await _totpService.IsTotpEnabledAsync(user!.Id))
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "TOTP is already enabled on your account. Disable it first to re-enrol."
                });
            }

            var result = await _totpService.CreateSetupAsync(user.Id, user.EmailAddress);

            return Ok(new TotpSetupResponseModel
            {
                Secret = result.Secret,
                ProvisioningUri = result.ProvisioningUri
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set up TOTP");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to set up TOTP"
            });
        }
    }


    [HttpPost("enable")]
    [ProducesResponseType(typeof(TotpEnableResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TotpEnableResponseModel>> EnableTotp(
        [FromBody] TotpVerifyRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var result = await _totpService.EnablePendingAsync(user!.Id, request.Code);

            if (result == null)
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "Invalid code or no pending setup found. " +
                             "Check your authenticator app and try again."
                });
            }

            return Ok(new TotpEnableResponseModel
            {
                IsEnabled = true,
                RecoveryCodes = result.RecoveryCodes
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to enable TOTP");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to enable TOTP"
            });
        }
    }


    [HttpPost("disable")]
    [ProducesResponseType(typeof(TotpStatusResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TotpStatusResponseModel>> DisableTotp(
        [FromBody] TotpVerifyRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var success = await _totpService.DisableAsync(user!.Id, request.Code);

            if (!success)
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "Invalid code or TOTP is not currently enabled. " +
                             "You must verify your identity to disable two-factor authentication."
                });
            }

            return Ok(new TotpStatusResponseModel { IsEnabled = false });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to disable TOTP");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to disable TOTP"
            });
        }
    }


    [HttpGet("recovery-codes/count")]
    [ProducesResponseType(typeof(RecoveryCodeCountResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RecoveryCodeCountResponseModel>> GetRecoveryCodeCount()
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var remaining = await _totpService.GetRemainingRecoveryCodeCountAsync(user!.Id);

            return Ok(new RecoveryCodeCountResponseModel { Remaining = remaining });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get recovery code count");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to get recovery code count"
            });
        }
    }


    [HttpPost("recovery-codes/regenerate")]
    [ProducesResponseType(typeof(RecoveryCodesResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RecoveryCodesResponseModel>> RegenerateRecoveryCodes(
        [FromBody] TotpVerifyRequestModel request
    )
    {
        try
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var codes = await _totpService.RegenerateRecoveryCodesAsync(user!.Id, request.Code);

            if (codes == null)
            {
                return BadRequest(new FailureResponseModel
                {
                    Detail = "Invalid TOTP code. You must verify your identity to regenerate recovery codes."
                });
            }

            return Ok(new RecoveryCodesResponseModel { RecoveryCodes = codes });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to regenerate recovery codes");
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
            {
                Detail = "Failed to regenerate recovery codes"
            });
        }
    }
}
