using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserRefresh;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Implementations;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Interfaces;
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
            IConfiguration configuration,
            AuxiliumDbContext db,
            ILogger<AuthenticationAuthenticatedController> logger,

            ICaptchaService captchaService,
            IPasswordService passwordService,
            ITokenService tokenService
            )
            : base(configuration, db, logger)
        {
            _captchaService = captchaService;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }


        [HttpPost("refresh")]
        [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserLoginResponseModel>> Refresh(
            [FromBody] UserRefreshTokenRequestModel request)
        {
            try
            {
                var strategy = this.Db.Database.CreateExecutionStrategy();
                string? accessToken = null;
                string? newRefreshToken = null;
                int expiresIn = 0;

                // hash the provided refresh token
                var tokenHash = HashingUtilities.SHA256Hash(request.RefreshToken);

                // verify the refresh token and get user
                var refreshToken = await this.Db.RefreshTokens
                    .Include(rt => rt.CreatedByUser)
                    .FirstOrDefaultAsync(rt =>
                        rt.TokenHash == tokenHash &&
                        rt.ExpiresAt > DateTime.UtcNow);

                if (refreshToken == null || refreshToken.CreatedByUser == null)
                {
                    throw new UnauthorizedAccessException("Invalid or expired refresh token");
                }

                var user = refreshToken.CreatedByUser;

                // create new access and refresh tokens
                var userData = new Dictionary<string, object>
                {
                    ["id"] = user.Id
                };
                accessToken = _tokenService.CreateAccessToken(userData);
                newRefreshToken = _tokenService.CreateRefreshToken(userData);

                // update the refresh token
                var newTokenHash = HashingUtilities.SHA256Hash(newRefreshToken);
                var newExpiresAt = DateTime.UtcNow.AddDays(
                    this.Configuration.JWT.RefreshTokenExpirationInDays
                );

                refreshToken.TokenHash = newTokenHash;
                refreshToken.ExpiresAt = newExpiresAt;

                await this.Db.SaveChangesAsync();

                expiresIn = this.Configuration.JWT.AccessTokenExpirationInMinutes * 60;

                this.Logger.LogInformation("Refresh token renewed for user {UserId}", user.Id);

                return Ok(new UserLoginResponseModel
                {
                    AccessToken = accessToken!,
                    RefreshToken = newRefreshToken!,
                    ExpiresIn = expiresIn
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new FailureResponseModel { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "An error occurred during token refresh"
                });
            }
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
