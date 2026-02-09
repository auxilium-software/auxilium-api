using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserRefresh;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Implementations;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/authentication")]
    [Tags("Authentication")]
    public class AuthenticationAuthenticatedController : LoggedInControllerBase
    {
        private readonly ICaptchaService _captchaService;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthenticationAuthenticatedController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWafService waf,
            ILogger<AuthenticationAuthenticatedController> logger,
            ITotpService totpService,

            ICaptchaService captchaService,
            IPasswordService passwordService,
            ITokenService tokenService
            )
            : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _captchaService = captchaService;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }



        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<SuccessResponseModel>> Logout()
        {
            try
            {
                // grab the current user id from token
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new FailureResponseModel { Detail = "User ID not found in token" });
                }

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid user ID format" });
                }

                var strategy = this.Db.Database.CreateExecutionStrategy();

                // delete all the refresh tokens for this user
                var tokens = this.Db.RefreshTokens.Where(rt => rt.CreatedBy == userGuid);
                this.Db.RefreshTokens.RemoveRange(tokens);

                await this.Db.SaveChangesAsync();

                this.Logger.LogInformation("User {UserId} logged out successfully", userGuid);

                return Ok(new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error during logout");
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "An error occurred during logout"
                });
            }
        }
    }
}
