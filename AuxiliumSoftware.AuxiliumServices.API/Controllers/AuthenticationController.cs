using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserRefresh;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserRegistration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.Services;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("/api/v3/authentication")]
[Tags("Authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly ConfigurationStructure _configuration;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly AuxiliumDbContext _db;

    private readonly ICaptchaService _captchaService;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthenticationController(
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<AuthenticationController> logger,

        ICaptchaService captchaService,
        IPasswordService passwordService,
        ITokenService tokenService
        )
    {
        _configuration = configuration.Get<ConfigurationStructure>();
        _logger = logger;
        _db = db;

        _captchaService = captchaService;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserRegistrationResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserRegistrationResponseModel>> Register(
        [FromBody] UserRegistrationRequestModel request)
    {
        try
        {
            // verify the reCAPTCHA token
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel { Detail = "reCAPTCHA token is required" });
            }

            string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            var strategy = _db.Database.CreateExecutionStrategy();
            Guid? createdUserId = null;

            await strategy.ExecuteAsync(async () =>
            {
                // check if user already exists
                var existingUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("Email address is already associated with an existing user account.");
                }

                // generate UUIDs
                var userId = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
                var caseId = UUIDUtilities.GenerateV5(DatabaseObjectType.Case);
                var caseClientId = UUIDUtilities.GenerateV5(DatabaseObjectType.CaseClient);

                // hash password
                var passwordHash = _passwordService.HashPassword(request.RawPassword);

                // create the user entity
                var user = new UserEntityModel
                {
                    Id = userId,
                    EmailAddress = request.EmailAddress,
                    PasswordHash = passwordHash,
                    FullName = request.FullName,
                    FullAddress = request.FullAddress,
                    TelephoneNumber = request.TelephoneNumber,
                    Gender = request.Gender,
                    DateOfBirth = DateOnly.Parse(request.DateOfBirth),
                    LanguagePreference = request.LanguagePreference,
                    HowDidYouFindOutAboutOurService = request.HowDidYouFindOutAboutOurService,
                    IsAdmin = false,
                    IsCaseWorker = false,
                    AllowLogin = true,
                    HasEmailAddressBeenVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                // create the case entity
                var caseEntity = new CaseEntityModel
                {
                    Id = caseId,
                    Title = request.CaseTitle,
                    Description = request.CaseDescription,
                    Sensitivity = CaseSensitivityEnum.Confidential,
                    Status = CaseStatusEnum.Open,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    LastUpdatedAt = DateTime.UtcNow,
                    LastUpdatedBy = userId
                };

                // add the user and the case to database
                _db.Users.Add(user);
                _db.Cases.Add(caseEntity);

                // add the user as a client of the case
                var caseClient = new CaseClientEntityModel
                {
                    Id = caseClientId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    CaseId = caseId,
                    UserId = userId
                };
                _db.CaseClients.Add(caseClient);

                // save all changes
                await _db.SaveChangesAsync();

                createdUserId = userId;
                _logger.LogInformation("User {UserId} registered successfully", userId);
            });

            return CreatedAtAction(
                nameof(Register),
                new UserRegistrationResponseModel
                {
                    Id = createdUserId!.Value
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new FailureResponseModel { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during registration"
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> Login(
        [FromBody] UserLoginRequestModel request)
    {
        try
        {
            // verify the reCAPTCHA token
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel { Detail = "reCAPTCHA token is required" });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            var strategy = _db.Database.CreateExecutionStrategy();
            string? accessToken = null;
            string? refreshToken = null;
            int expiresIn = 0;

            await strategy.ExecuteAsync(async () =>
            {
                // grab the user from the database
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress);

                // check if the user exists
                if (user == null)
                {
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                // check if the user is actually allowed to login
                if (!user.AllowLogin)
                {
                    throw new UnauthorizedAccessException("Account blocked from logging in by the Auxilium IT department.");
                }

                // verify the password
                if (!_passwordService.VerifyPassword(request.RawPassword, user.PasswordHash))
                {
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                // create the access and refresh tokens
                var userData = new Dictionary<string, object>
                {
                    ["id"] = user.Id
                };
                accessToken = _tokenService.CreateAccessToken(userData);
                refreshToken = _tokenService.CreateRefreshToken(userData);

                // delete expired refresh tokens
                var expiredTokens = _db.RefreshTokens
                    .Where(rt => rt.CreatedBy == user.Id && rt.ExpiresAt < DateTime.UtcNow);
                _db.RefreshTokens.RemoveRange(expiredTokens);

                // store the new refresh token
                var refreshTokenId = UUIDUtilities.GenerateV5(DatabaseObjectType.RefreshToken);
                var tokenHash = HashingUtilities.SHA256Hash(refreshToken);
                var expiresAtTime = DateTime.UtcNow.AddDays(this._configuration.JWT.RefreshTokenExpirationInDays);

                var refreshTokenEntity = new RefreshTokenEntityModel
                {
                    Id = refreshTokenId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.Id,
                    TokenHash = tokenHash,
                    ExpiresAt = expiresAtTime
                };
                _db.RefreshTokens.Add(refreshTokenEntity);

                await _db.SaveChangesAsync();

                expiresIn = this._configuration.JWT.AccessTokenExpirationInMinutes * 60;

                _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            });

            return Ok(new UserLoginResponseModel
            {
                AccessToken = accessToken!,
                RefreshToken = refreshToken!,
                ExpiresIn = expiresIn
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new FailureResponseModel { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during login"
            });
        }
    }






    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> Refresh(
        [FromBody] UserRefreshTokenRequestModel request)
    {
        try
        {
            var strategy = this._db.Database.CreateExecutionStrategy();
            string? accessToken = null;
            string? newRefreshToken = null;
            int expiresIn = 0;

            // hash the provided refresh token
            var tokenHash = HashingUtilities.SHA256Hash(request.RefreshToken);

            // verify the refresh token and get user
            var refreshToken = await this._db.RefreshTokens
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
                this._configuration.JWT.RefreshTokenExpirationInDays
            );

            refreshToken.TokenHash = newTokenHash;
            refreshToken.ExpiresAt = newExpiresAt;

            await this._db.SaveChangesAsync();

            expiresIn = this._configuration.JWT.AccessTokenExpirationInMinutes * 60;

            this._logger.LogInformation("Refresh token renewed for user {UserId}", user.Id);

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
            this._logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during token refresh"
            });
        }
    }
}
