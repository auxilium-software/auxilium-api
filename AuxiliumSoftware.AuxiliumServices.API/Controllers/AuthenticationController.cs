using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserLogin;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserRefresh;
using AuxiliumSoftware.AuxiliumServices.API.Models.UserRegistration;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

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
    private readonly IWebApplicationFirewallService _wafService;
    private readonly ITotpService _totpService;

    private readonly ICaptchaService _captchaService;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthenticationController(
        IConfiguration configuration,
        AuxiliumDbContext db,
        ILogger<AuthenticationController> logger,
        IWebApplicationFirewallService wafService,
        ITotpService totpService,

        ICaptchaService captchaService,
        IPasswordService passwordService,
        ITokenService tokenService
        )
    {
        _configuration = configuration.Get<ConfigurationStructure>();
        _logger = logger;
        _db = db;
        _wafService = wafService;
        _totpService = totpService;

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
        [FromBody] UserRegistrationRequestModel request
    )
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
                var userId = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.User);
                var caseId = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.Case);
                var caseClientId = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.Case_Client);

                // hash password
                var normalised = this._passwordService.NormalisePassword(request.RawPassword, request.PasswordSha512);
                var passwordHash = _passwordService.HashPassword(normalised);

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
                    IsAdministrator = false,
                    IsCaseWorker = false,
                    IsCaseWorkerManager = false,
                    AllowLogin = true,
                    MustChangePassword = false,
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
            return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> Login(
        [FromBody] UserLoginRequestModel request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel { Detail = "reCAPTCHA token is required" });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            var strategy = _db.Database.CreateExecutionStrategy();
            UserLoginResponseModel? response = null;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress);

            if (user == null)
            {
                await this._wafService.RecordFailedLoginAsync(
                    ipAddress: HttpContext.Connection.RemoteIpAddress,
                    attemptedEmail: request.EmailAddress,
                    user: null,
                    failureReason: LoginAttemptFailureReasonEnum.UserNotFound
                );
                return StatusCode(StatusCodes.Status401Unauthorized, new FailureResponseModel
                {
                    Detail = "Invalid credentials"
                });
            }

            var normalised = this._passwordService.NormalisePassword(request.RawPassword, request.PasswordSha512);
            if (!_passwordService.VerifyPassword(normalised, user.PasswordHash))
            {
                await this._wafService.RecordFailedLoginAsync(
                    ipAddress: HttpContext.Connection.RemoteIpAddress,
                    attemptedEmail: request.EmailAddress,
                    user: user,
                    failureReason: LoginAttemptFailureReasonEnum.InvalidPassword
                );
                return StatusCode(StatusCodes.Status401Unauthorized, new FailureResponseModel
                {
                    Detail = "Invalid credentials"
                });
            }

            if (!user.AllowLogin)
            {
                await this._wafService.RecordFailedLoginAsync(
                    ipAddress: HttpContext.Connection.RemoteIpAddress,
                    attemptedEmail: request.EmailAddress,
                    user: user,
                    failureReason: LoginAttemptFailureReasonEnum.AccountLocked
                );
                return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                {
                    Detail = "Account blocked from logging in by the Auxilium IT department."
                });
            }

            // TOTP enabled => return MFA session token instead
            if (user.TotpEnabled)
            {
                var userData = new Dictionary<string, object>
                {
                    ["id"] = user.Id
                };

                response = new UserLoginResponseModel
                {
                    MfaRequired = true,
                    MfaSessionToken = _tokenService.CreateMfaToken(userData)
                };

                _logger.LogInformation("MFA required for user {UserId}", user.Id);
                return Ok(response);
            }

            // no TOTP for this account => issue tokens directly
            response = await IssueTokensForUserAsync(user);
            _logger.LogInformation("User {UserId} logged in successfully", user.Id);


            await this._wafService.RecordSuccessfulLoginAsync(
                ipAddress: HttpContext.Connection.RemoteIpAddress,
                email: request.EmailAddress,
                user: user
            );

            return Ok(response);
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
    [HttpPost("verify-totp")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> VerifyTotp(
        [FromBody] VerifyTotpRequestModel request)
    {
        try
        {
            var userId = _tokenService.ValidateMfaToken(request.MfaSessionToken);
            if (userId == null)
            {
                return Unauthorized(new FailureResponseModel { Detail = "Invalid or expired MFA session" });
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            UserLoginResponseModel? response = null;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid session");
            }

            if (!user.AllowLogin)
            {
                throw new UnauthorizedAccessException("Account blocked from logging in by the Auxilium IT department.");
            }

            if (string.IsNullOrEmpty(user.TotpSecret))
            {
                throw new UnauthorizedAccessException("TOTP not configured for this account");
            }

            if (!(await _totpService.ValidateUserTotpAsync(user.Id, request.TotpCode)))
            {
                throw new UnauthorizedAccessException("Invalid TOTP code");
            }

            response = await IssueTokensForUserAsync(user);
            _logger.LogInformation("User {UserId} completed MFA login", user.Id);

            await this._wafService.RecordSuccessfulLoginAsync(
                ipAddress: HttpContext.Connection.RemoteIpAddress,
                email: user.EmailAddress,
                user: user
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new FailureResponseModel { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TOTP verification");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during verification"
            });
        }
    }


    [AllowAnonymous]
    [HttpPost("verify-recovery-code")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> VerifyRecoveryCode(
    [FromBody] VerifyRecoveryCodeRequestModel request)
    {
        try
        {
            var userId = _tokenService.ValidateMfaToken(request.MfaSessionToken);
            if (userId == null)
            {
                return Unauthorized(new FailureResponseModel { Detail = "Invalid or expired MFA session" });
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            UserLoginResponseModel? response = null;

            await strategy.ExecuteAsync(async () =>
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    throw new UnauthorizedAccessException("Invalid session");
                }

                if (!user.AllowLogin)
                {
                    throw new UnauthorizedAccessException("Account blocked from logging in by the Auxilium IT department.");
                }

                // hash the provided recovery code and look for a match
                var codeHash = HashingUtilities.SHA256Hash(request.RecoveryCode);

                var recoveryCode = await _db.TotpRecoveryCodes
                    .FirstOrDefaultAsync(rc =>
                        rc.CreatedBy == userId &&
                        rc.CodeHash == codeHash &&
                        !rc.IsUsed);

                if (recoveryCode == null)
                {
                    throw new UnauthorizedAccessException("Invalid recovery code");
                }

                // mark the recovery code as used
                recoveryCode.IsUsed = true;
                recoveryCode.UsedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                response = await IssueTokensForUserAsync(user);

                _logger.LogInformation(
                    "User {UserId} completed MFA login using recovery code {CodeId}",
                    user.Id,
                    recoveryCode.Id
                );
            });

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new FailureResponseModel { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recovery code verification");
            return StatusCode(500, new FailureResponseModel
            {
                Detail = "An error occurred during verification"
            });
        }
    }
















    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseModel>> Refresh(
        [FromBody] UserRefreshTokenRequestModel request
    )
    {
        try
        {
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
                    rt.ExpiresAt > DateTime.UtcNow
                );

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
                ExpiresIn = expiresIn,
                MfaRequired = false,
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













    private async Task<UserLoginResponseModel> IssueTokensForUserAsync(UserEntityModel user)
    {
        var userData = new Dictionary<string, object>
        {
            ["id"] = user.Id
        };

        var accessToken = _tokenService.CreateAccessToken(userData);
        var refreshToken = _tokenService.CreateRefreshToken(userData);

        // remove expired refresh tokens
        var expiredTokens = _db.RefreshTokens
            .Where(rt => rt.CreatedBy == user.Id && rt.ExpiresAt < DateTime.UtcNow);
        _db.RefreshTokens.RemoveRange(expiredTokens);

        // store the new refresh token
        var refreshTokenId = UUIDUtilities.GenerateV5(DatabaseObjectTypeEnum.User_RefreshToken);
        var tokenHash = HashingUtilities.SHA256Hash(refreshToken);
        var expiresAtTime = DateTime.UtcNow.AddDays(_configuration.JWT.RefreshTokenExpirationInDays);

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

        return new UserLoginResponseModel
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _configuration.JWT.AccessTokenExpirationInMinutes * 60,
            MfaRequired = false,
        };
    }
}
