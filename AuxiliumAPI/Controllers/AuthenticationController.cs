using AuxiliumAPI.Common.DataStructures.CouchDB;
using AuxiliumAPI.Common.DataStructures.MariaDB;
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

namespace AuxiliumAPI.Controllers;

[ApiController]
[Route("/api/v3/authentication")]
[Tags("Authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration Configuration;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IMariaDbService _mariaDb;
    private readonly ICouchDbService _couchDb;
    private readonly ICaptchaService _captchaService;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthenticationController(
        IConfiguration configuration,
        ILogger<AuthenticationController> logger,
        IMariaDbService mariaDb,
        ICouchDbService couchDb,
        ICaptchaService captchaService,
        IPasswordService passwordService,
        ITokenService tokenService
        )
    {
        this.Configuration = configuration;
        this._logger = logger;
        this._mariaDb = mariaDb;
        this._couchDb = couchDb;
        this._captchaService = captchaService;
        this._passwordService = passwordService;
        this._tokenService = tokenService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserRegistrationResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserRegistrationResponseModel>> Register(
        [FromBody] UserRegistrationRequestModel request)
    {
        await using var transaction = await _mariaDb.BeginTransactionAsync();

        try
        {
            // make sure the recaptcha token exists
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel() { Detail = "reCAPTCHA token is required" });
            }

            // verify recaptcha
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            // check if user already exists in mariadb
            var existingUser = await _mariaDb.ExecuteScalarAsync<string>(
                "SELECT id FROM users WHERE email_address = @email",
                new { email = request.EmailAddress }
            );

            if (existingUser != null)
            {
                return Conflict(new FailureResponseModel() { Detail = "Email address is already associated with an existing user account." });
            }

            // generate uuids
            Guid userID = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
            Guid caseID = UUIDUtilities.GenerateV5(DatabaseObjectType.Case);

            // hash psssword
            var passwordHash = _passwordService.HashPassword(request.RawPassword);

            // create the user in mariadb
            await _mariaDb.ExecuteAsync(
                """
                INSERT INTO users (id, email_address, password_hash) 
                VALUES (@userId, @emailAddress, @passwordHash)
                """,
                new
                {
                    userID,
                    emailAddress = request.EmailAddress,
                    passwordHash
                }
            );

            // create the user and case documents
            var userDoc = new UserDocumentStructure
            {
                Id = userID.ToString(),
                CreatedBy = userID,
                FullName = request.FullName,
                FullAddress = request.FullAddress,
                TelephoneNumber = request.TelephoneNumber,
                Gender = request.Gender,
                // DateOfBirth = request.DateOfBirth,
                HowDidYouFindOutAboutOurService = request.HowDidYouFindOutAboutOurService
            };
            var caseDoc = new CaseDocumentStructure
            {
                Id = caseID.ToString(),
                CreatedBy = userID,
                Title = request.CaseTitle,
                Description = request.CaseDescription,
                Sensitivity = CaseSensitivityEnum.Confidential,
                Status = CaseStatusEnum.Open,
                Clients = new List<Guid> { userID }
            };

            // save documents to couchdb
            await _couchDb.SaveDocumentAsync(this.Configuration!["Databases:CouchDB:Databases:Users"]!, userDoc);
            await _couchDb.SaveDocumentAsync(this.Configuration!["Databases:CouchDB:Databases:Cases"]!, caseDoc);

            // commit mariadb transaction
            await transaction.CommitAsync();

            // return success response
            return CreatedAtAction(
                nameof(Register),
                new UserRegistrationResponseModel
                {
                    Id = userID,
                }
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during user registration");
            throw;
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserLoginResponseModel>> Login(
        [FromBody] UserLoginRequestModel request)
    {
        await using var transaction = await _mariaDb.BeginTransactionAsync();

        try
        {
            // make sure the recaptcha token exists
            if (string.IsNullOrEmpty(request.RecaptchaToken))
            {
                return BadRequest(new FailureResponseModel() { Detail = "reCAPTCHA token is required" });
            }

            // verify recaptcha
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _captchaService.VerifyRecaptchaAsync(request.RecaptchaToken, clientIp);

            // get user from mariadb
            var user = await _mariaDb.QuerySingleOrDefaultAsync<UserRowStructure>(
                "SELECT * FROM users WHERE email_address = @email",
                new { email = request.EmailAddress }
            );

            // check if the user exists
            if (user == null)
            {
                return Unauthorized(new { detail = "Invalid credentials" });
            }

            // check if the user is allowed to log in
            if (!user.allow_login)
            {
                return Unauthorized(new { detail = "Account blocked from logging in by the Auxilium IT department." });
            }

            // verify the password
            if (!_passwordService.VerifyPassword(request.RawPassword, user.password_hash))
            {
                return Unauthorized(new { detail = "Invalid credentials" });
            }

            // create access and refresh tokens
            var userData = new Dictionary<string, object>
            {
                ["id"] = user.id
            };
            var accessToken = _tokenService.CreateAccessToken(userData);
            var refreshToken = _tokenService.CreateRefreshToken(userData);

            // remove expired refresh tokens from mariadb
            await _mariaDb.ExecuteAsync(
                """
                DELETE FROM refresh_tokens 
                WHERE user_id = @userId AND expires_at < NOW()
                """,
                new { userId = user.id }
            );

            // store the newly created refresh token in mariadb
            var tokenHash = HashingUtilities.SHA256Hash(refreshToken);
            var expiresAt = DateTime.UtcNow.AddDays(this.Configuration.GetValue<int>("JWT:RefreshTokenExpireDays"));
            await _mariaDb.ExecuteAsync(
                """
                INSERT INTO refresh_tokens (user_id, token_hash, expires_at) 
                VALUES (@userId, @tokenHash, @expiresAt)
                """,
                new
                {
                    userId = user.id,
                    tokenHash,
                    expiresAt
                }
            );
            await transaction.CommitAsync();

            // return
            return Ok(new UserLoginResponseModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = this.Configuration.GetValue<int>("JWT:AccessTokenExpireMinutes") * 60
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during user login");
            throw;
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(UserLoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserLoginResponseModel>> Refresh(
        [FromBody] UserRefreshTokenRequestModel request)
    {
        await using var transaction = await _mariaDb.BeginTransactionAsync();

        try
        {
            // hash the provided refresh token
            var tokenHash = HashingUtilities.SHA256Hash(request.RefreshToken);

            // verify refresh token and get user from mariadb
            var user = await _mariaDb.QuerySingleOrDefaultAsync<UserRowStructure>(
                """
                SELECT u.* 
                FROM refresh_tokens rt
                INNER JOIN users u ON rt.user_id = u.id
                WHERE rt.token_hash = @tokenHash AND rt.expires_at > NOW()
                """,
                new { tokenHash }
            );

            // if the refresh token//user is not found, return unauthorized
            if (user == null)
            {
                return Unauthorized(new { detail = "Invalid or expired refresh token" });
            }

            // create new access and refresh tokens
            var userData = new Dictionary<string, object>
            {
                ["id"] = user.id
            };
            var accessToken = _tokenService.CreateAccessToken(userData);
            var newRefreshToken = _tokenService.CreateRefreshToken(userData);

            // update the refresh token in mariadb
            var newTokenHash = HashingUtilities.SHA256Hash(newRefreshToken);
            var newExpiresAt = DateTime.UtcNow.AddDays(this.Configuration.GetValue<int>("JWT:RefreshTokenExpireDays"));
            await _mariaDb.ExecuteAsync(
                """
                UPDATE refresh_tokens
                SET token_hash = @newTokenHash, expires_at = @expiresAt
                WHERE token_hash = @oldTokenHash
                """,
                new
                {
                    newTokenHash,
                    expiresAt = newExpiresAt,
                    oldTokenHash = tokenHash
                }
            );
            await transaction.CommitAsync();

            // return
            return Ok(new UserLoginResponseModel
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = this.Configuration.GetValue<int>("JWT:AccessTokenExpireMinutes") * 60
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<SuccessResponseModel>> Logout()
    {
        await using var transaction = await _mariaDb.BeginTransactionAsync();

        try
        {
            // get current user id from token
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new FailureResponseModel() { Detail = "User ID not found in token" });
            }
            if (!Guid.TryParse(userId, out _))
            {
                return BadRequest(new FailureResponseModel() { Detail = "You must provide a valid UUID" });
            }

            // remove all refresh tokens from mariadb for that user
            await _mariaDb.ExecuteAsync(
                "DELETE FROM refresh_tokens WHERE user_id = @userId",
                new { userId }
            );
            await transaction.CommitAsync();

            // return
            return Ok(new SuccessResponseModel());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }
}
