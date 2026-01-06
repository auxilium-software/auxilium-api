using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Common.Enumerators;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.Case;
using AuxiliumAPI.Models.UserLogin;
using AuxiliumAPI.Models.UserRefresh;
using AuxiliumAPI.Models.UserRegistration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/authentication")]
[Tags("Authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly AuxiliumDbContext _db;
    private readonly ICaptchaService _captchaService;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthenticationController(
        IConfiguration configuration,
        ILogger<AuthenticationController> logger,
        AuxiliumDbContext db,
        ICaptchaService captchaService,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
        _captchaService = captchaService;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserRegistrationResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserRegistrationResponseModel>> Register(
        [FromBody] UserRegistrationRequestModel request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // verify tje reCAPTCHA token
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel { Detail = "reCAPTCHA token is required" });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            // check if user already exists
            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress);

            if (existingUser != null)
            {
                return Conflict(new FailureResponseModel
                {
                    Detail = "Email address is already associated with an existing user account."
                });
            }

            // generate UUIDs
            var userId = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
            var caseId = UUIDUtilities.GenerateV5(DatabaseObjectType.Case);

            // hash password
            var passwordHash = _passwordService.HashPassword(request.RawPassword);

            // create the user entity
            var user = new UserModel
            {
                Id = userId,
                EmailAddress = request.EmailAddress,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                FullAddress = request.FullAddress,
                TelephoneNumber = request.TelephoneNumber,
                Gender = request.Gender,
                DateOfBirth = DateOnly.Parse(request.DateOfBirth),
                HowDidYouFindOutAboutOurService = request.HowDidYouFindOutAboutOurService,
                IsAdmin = false,
                IsCaseWorker = false,
                AllowLogin = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            // create the case entity
            var caseEntity = new CaseModel
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
            var caseClient = new CaseClientModel
            {
                CaseId = caseId,
                UserId = userId
            };
            _db.CaseClients.Add(caseClient);

            // save all changes
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} registered successfully", userId);

            return CreatedAtAction(
                nameof(Register),
                new UserRegistrationResponseModel
                {
                    Id = userId
                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during registration"
            });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> Login(
        [FromBody] UserLoginRequestModel request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // verify the reCAPTCHA token
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel { Detail = "reCAPTCHA token is required" });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            // grab the user from the database
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress);

            // check if the user exists
            if (user == null)
            {
                return Unauthorized(new FailureResponseModel { Detail = "Invalid credentials" });
            }

            // check if the user is actually allowed to login
            if (!user.AllowLogin)
            {
                return Unauthorized(new FailureResponseModel
                {
                    Detail = "Account blocked from logging in by the Auxilium IT department."
                });
            }

            // verify the password
            if (!_passwordService.VerifyPassword(request.RawPassword, user.PasswordHash))
            {
                return Unauthorized(new FailureResponseModel { Detail = "Invalid credentials" });
            }

            // create the access and refresh tokens
            var userData = new Dictionary<string, object>
            {
                ["id"] = user.Id
            };
            var accessToken = _tokenService.CreateAccessToken(userData);
            var refreshToken = _tokenService.CreateRefreshToken(userData);

            // delete expired refresh tokens
            var expiredTokens = _db.RefreshTokens
                .Where(rt => rt.CreatedBy == user.Id && rt.ExpiresAt < DateTime.UtcNow);
            _db.RefreshTokens.RemoveRange(expiredTokens);

            // store the new refresh token
            var tokenHash = HashingUtilities.SHA256Hash(refreshToken);
            var expiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JWT:RefreshTokenExpireDays"));

            var refreshTokenEntity = new RefreshTokenModel
            {
                CreatedBy = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt
            };
            _db.RefreshTokens.Add(refreshTokenEntity);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return Ok(new UserLoginResponseModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _configuration.GetValue<int>("JWT:AccessTokenExpireMinutes") * 60
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during login"
            });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> Refresh(
        [FromBody] UserRefreshTokenRequestModel request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // hash the provided refresh token
            var tokenHash = HashingUtilities.SHA256Hash(request.RefreshToken);

            // verify the refresh token and get user
            var refreshToken = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt =>
                    rt.TokenHash == tokenHash &&
                    rt.ExpiresAt > DateTime.UtcNow);

            if (refreshToken == null || refreshToken.User == null)
            {
                return Unauthorized(new FailureResponseModel
                {
                    Detail = "Invalid or expired refresh token"
                });
            }

            var user = refreshToken.User;

            // create new access and refresh tokens
            var userData = new Dictionary<string, object>
            {
                ["id"] = user.Id
            };
            var accessToken = _tokenService.CreateAccessToken(userData);
            var newRefreshToken = _tokenService.CreateRefreshToken(userData);

            // update the refresh token
            var newTokenHash = HashingUtilities.SHA256Hash(newRefreshToken);
            var newExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JWT:RefreshTokenExpireDays"));

            refreshToken.TokenHash = newTokenHash;
            refreshToken.ExpiresAt = newExpiresAt;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Refresh token renewed for user {UserId}", user.Id);

            return Ok(new UserLoginResponseModel
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _configuration.GetValue<int>("JWT:AccessTokenExpireMinutes") * 60
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during token refresh");
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
        await using var transaction = await _db.Database.BeginTransactionAsync();

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

            // delete all the refresh tokens for this user
            var tokens = _db.RefreshTokens.Where(rt => rt.CreatedBy == userGuid);
            _db.RefreshTokens.RemoveRange(tokens);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} logged out successfully", userGuid);

            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during logout"
            });
        }
    }
}
