using AuxiliumAPI.Common.ControllerBases;
using AuxiliumAPI.Common.DataStructures.CouchDB.SubStructures;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Models;
using AuxiliumAPI.Models.AdditionalProperty;
using AuxiliumAPI.Models.Case;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers
{
    [ApiController]
    [Route("/api/v3/users/{userId}/additional_properties")]
    [Tags("Users")]
    public class UserPropertiesController : LoggedInControllerBase
    {
        private readonly IUserDocumentService _userDocService;
        private readonly ILogger<UserPropertiesController> _logger;

        public UserPropertiesController(
            IUserDocumentService userDocService,
            ILogger<UserPropertiesController> logger,
            IMariaDbService mariaDb)
            : base(mariaDb, logger)
        {
            _userDocService = userDocService;
            _logger = logger;
        }

        [HttpPost("{propertyName}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> CreateProperty(
            string userId,
            string propertyName,
            [FromBody] AdditionalPropertyCreationRequestModel request)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // checks to make sure the given user id is valid
                if (!Guid.TryParse(userId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
                }

                // only allow admins or themselves to create additional properties
                if (!user!.is_admin && user.id.ToString() != userId)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You can only modify your own properties"
                    });
                }

                // normalise property names
                var (storageKey, autoDisplayName) = PropertyNameHandlingUtilities.HandlePropertyName(propertyName);
                var finalDisplayName = request.DisplayName ?? autoDisplayName;

                // grab existing additional properties
                var properties = await _userDocService.GetAdditionalPropertiesAsync(Guid.Parse(userId));

                // check if the proposed new additional property already exists
                if (properties.ContainsKey(storageKey))
                {
                    return Conflict(new FailureResponseModel
                    {
                        Detail = "Property already exists. Use PATCH to update."
                    });
                }

                // create the substructure to save into the user document
                var propVal = new AdditionalPropertySubStructure()
                {
                    Content = request.Content ?? string.Empty,
                    ContentType = request.ContentType ?? "Text",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.id,
                    Id = Guid.NewGuid(),
                    OriginalName = propertyName,
                    PrettyName = finalDisplayName,
                    UrlSlug = PropertyNameHandlingUtilities.NormalizeKey(finalDisplayName)
                };

                // save the additional property
                await _userDocService.SaveAdditionalPropertyAsync(Guid.Parse(userId), storageKey, propVal);

                _logger.LogInformation(
                    "Created property {PropertyName} for user {UserId}",
                    storageKey, userId
                );

                // return
                return StatusCode(201, new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create property {PropertyName} for user {UserId}", propertyName, userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to create property"
                });
            }
        }

        [HttpPatch("{propertyName}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> UpdateProperty(
            string userId,
            string propertyName,
            [FromBody] AdditionalPropertyCreationRequestModel request)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // make sure the user id is a valid uuid
                if (!Guid.TryParse(userId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
                }

                // only allow admins or themselves to modify additional properties
                if (!user!.is_admin && user.id.ToString() != userId)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You can only modify your own properties"
                    });
                }

                // normalise property names
                var (storageKey, autoDisplayName) = PropertyNameHandlingUtilities.HandlePropertyName(propertyName);

                // grab the existing additional properties
                var properties = await _userDocService.GetAdditionalPropertiesAsync(Guid.Parse(userId));

                // make sure the additional property actually exists
                if (!properties.ContainsKey(storageKey))
                {
                    return NotFound(new FailureResponseModel
                    {
                        Detail = "Property not found. Use POST to create."
                    });
                }

                // update the additional property
                var finalDisplayName = request.DisplayName ?? autoDisplayName;

                // create the substructure to save into the user document
                var propVal = new AdditionalPropertySubStructure()
                {
                    Content = request.Content ?? string.Empty,
                    ContentType = request.ContentType ?? "Text",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.id,
                    Id = Guid.NewGuid(),
                    OriginalName = propertyName,
                    PrettyName = finalDisplayName,
                    UrlSlug = PropertyNameHandlingUtilities.NormalizeKey(finalDisplayName)
                };

                // save the additional property
                await _userDocService.SaveAdditionalPropertyAsync(
                    Guid.Parse(userId),
                    storageKey,
                    propVal
                );

                _logger.LogInformation(
                    "Updated property {PropertyName} for user {UserId}",
                    storageKey, userId
                );

                // return
                return Ok(new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update property {PropertyName} for user {UserId}", propertyName, userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to update property"
                });
            }
        }

        [HttpDelete("{propertyName}")]
        [ProducesResponseType(typeof(SuccessResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessResponseModel>> DeleteProperty(
            string userId,
            string propertyName)
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                // check to make sure that the given user id is a valid uuid
                if (!Guid.TryParse(userId, out _))
                {
                    return BadRequest(new FailureResponseModel { Detail = "Invalid user ID" });
                }

                // only allow admins or themselves to delete additional properties
                if (!user!.is_admin && user.id.ToString() != userId)
                {
                    return StatusCode(403, new FailureResponseModel
                    {
                        Detail = "You can only modify your own properties"
                    });
                }

                // grab the key for the given additional property name
                var (storageKey, _) = PropertyNameHandlingUtilities.HandlePropertyName(propertyName);

                // grab all the existing additional properties for this user
                var properties = await _userDocService.GetAdditionalPropertiesAsync(Guid.Parse(userId));

                // make sure the additional property actually exists
                if (!properties.ContainsKey(storageKey))
                {
                    return NotFound(new FailureResponseModel
                    {
                        Detail = "Property not found"
                    });
                }

                // delete the additional property
                await _userDocService.DeleteAdditionalPropertyAsync(Guid.Parse(userId), storageKey);

                _logger.LogInformation(
                    "Deleted property {PropertyName} from user {UserId}",
                    storageKey, userId
                );

                // return
                return Ok(new SuccessResponseModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete property {PropertyName} from user {UserId}", propertyName, userId);
                return StatusCode(500, new FailureResponseModel
                {
                    Detail = "Failed to delete property"
                });
            }
        }
    }
}
