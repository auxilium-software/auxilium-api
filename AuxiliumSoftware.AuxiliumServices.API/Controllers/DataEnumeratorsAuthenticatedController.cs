using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Mappers;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorTranslation;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValueTranslation;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/data-enumerators")]
    [Tags("Data Enumerators")]
    public class DataEnumeratorsAuthenticatedController : LoggedInControllerBase
    {
        private readonly IDataEnumeratorService _dataEnumeratorService;

        public DataEnumeratorsAuthenticatedController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<DataEnumeratorsAuthenticatedController> logger,
            ITotpService totpService,
            IDataEnumeratorService dataEnumeratorService
        ) : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
            _dataEnumeratorService = dataEnumeratorService;
        }

        #region ========================= ENUMERATOR TYPE ENDPOINTS =========================

        [HttpGet("all")]
        [ProducesResponseType(typeof(List<DataEnumeratorResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DataEnumeratorResponseModel>>> GetAllEnumerators(
            [FromQuery] bool includeInactive = false,
            [FromQuery] string? locale = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                if (includeInactive && !user!.IsAdministrator)
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only administrators can view inactive enumerators."
                    });

                var enumerators = await _dataEnumeratorService.GetAllEnumeratorsAsync(includeInactive, ct);
                var resolvedLocale = locale ?? user!.LanguagePreference;

                var response = enumerators
                    .Select(e => DataEnumeratorMapper.ToResponseModel(e, resolvedLocale))
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get all enumerators");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DataEnumeratorResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorResponseModel>> GetEnumerator(
            [FromRoute] Guid id,
            [FromQuery] bool includeInactive = false,
            [FromQuery] string? locale = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                if (includeInactive && !user!.IsAdministrator)
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only administrators can view inactive enumerators."
                    });

                var enumerator = await _dataEnumeratorService.GetEnumeratorAsync(id, ct);
                if (enumerator == null)
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                    {
                        Detail = "Enumerator not found."
                    });

                if (!enumerator.IsActive && !user!.IsAdministrator)
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "This enumerator is inactive."
                    });

                return StatusCode(StatusCodes.Status200OK, DataEnumeratorMapper.ToResponseModel(enumerator, locale ?? user!.LanguagePreference));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(DataEnumeratorResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorResponseModel>> CreateEnumerator(
            [FromBody] DataEnumeratorCreationRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var enumerator = await _dataEnumeratorService.CreateEnumeratorAsync(
                    request.Name,
                    request.Description,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status201Created, DataEnumeratorMapper.ToResponseModel(enumerator));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create enumerator");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}")]
        [ProducesResponseType(typeof(DataEnumeratorResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorResponseModel>> UpdateEnumerator(
            [FromRoute] Guid id,
            [FromBody] DataEnumeratorUpdateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var enumerator = await _dataEnumeratorService.UpdateEnumeratorAsync(
                    id,
                    request.Name,
                    request.Description,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status200OK, DataEnumeratorMapper.ToResponseModel(enumerator));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}/active")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetEnumeratorActive(
            [FromRoute] Guid id,
            [FromBody] DataEnumeratorSetActiveRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                await _dataEnumeratorService.SetEnumeratorActiveAsync(id, request.IsActive, user!, ct);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to set active state on enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        #endregion

        #region ========================= ENUMERATOR TYPE TRANSLATION ENDPOINTS =========================

        [HttpGet("{id:guid}/translations")]
        [ProducesResponseType(typeof(List<DataEnumeratorTranslationResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DataEnumeratorTranslationResponseModel>>> GetEnumeratorTranslations(
            [FromRoute] Guid id,
            CancellationToken ct = default
        )
        {
            try
            {
                var (_, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var translations = await _dataEnumeratorService.GetEnumeratorTranslationsAsync(id, ct);

                var response = translations
                    .Select(DataEnumeratorMapper.ToEnumeratorTranslationResponseModel)
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get translations for enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("{id:guid}/translations")]
        [ProducesResponseType(typeof(DataEnumeratorTranslationResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorTranslationResponseModel>> CreateEnumeratorTranslation(
            [FromRoute] Guid id,
            [FromBody] DataEnumeratorTranslationCreateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var translation = await _dataEnumeratorService.CreateEnumeratorTranslationAsync(
                    id,
                    request.LanguageCode,
                    request.Translation,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status201Created,
                    DataEnumeratorMapper.ToEnumeratorTranslationResponseModel(translation));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator not found."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "A translation for that language already exists on this enumerator."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create translation for enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}/translations/{translationId:guid}")]
        [ProducesResponseType(typeof(DataEnumeratorTranslationResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorTranslationResponseModel>> UpdateEnumeratorTranslation(
            [FromRoute] Guid id,
            [FromRoute] Guid translationId,
            [FromBody] DataEnumeratorTranslationUpdateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var translation = await _dataEnumeratorService.UpdateEnumeratorTranslationAsync(
                    translationId,
                    request.LanguageCode,
                    request.Translation,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status200OK,
                    DataEnumeratorMapper.ToEnumeratorTranslationResponseModel(translation));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Translation not found."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "A translation for that language already exists on this enumerator."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update enumerator translation {TranslationId}", translationId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpDelete("{id:guid}/translations/{translationId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteEnumeratorTranslation(
            [FromRoute] Guid id,
            [FromRoute] Guid translationId,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                await _dataEnumeratorService.DeleteEnumeratorTranslationAsync(translationId, ct);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Translation not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete enumerator translation {TranslationId}", translationId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        #endregion

        #region ========================= ENUMERATOR VALUE ENDPOINTS =========================

        [HttpGet("{id:guid}/values")]
        [ProducesResponseType(typeof(List<DataEnumeratorValueResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DataEnumeratorValueResponseModel>>> GetValues(
            [FromRoute] Guid id,
            [FromQuery] bool includeInactive = false,
            [FromQuery] string? locale = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                if (includeInactive && !user!.IsAdministrator)
                    return StatusCode(StatusCodes.Status403Forbidden, new FailureResponseModel
                    {
                        Detail = "Only administrators can view inactive values."
                    });

                var values = await _dataEnumeratorService.GetValuesAsync(id, includeInactive, ct);
                var resolvedLocale = locale ?? user!.LanguagePreference;

                var response = values
                    .Select(v => DataEnumeratorMapper.ToValueResponseModel(v, resolvedLocale))
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get values for enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("{id:guid}/values")]
        [ProducesResponseType(typeof(DataEnumeratorValueResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorValueResponseModel>> CreateValue(
            [FromRoute] Guid id,
            [FromBody] DataEnumeratorValueCreateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var value = await _dataEnumeratorService.CreateValueAsync(
                    id,
                    request.DisplayName,
                    user!,
                    request.SortOrder,
                    ct
                );

                return StatusCode(StatusCodes.Status201Created, DataEnumeratorMapper.ToValueResponseModel(value));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create value under enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}/values/{valueId:guid}")]
        [ProducesResponseType(typeof(DataEnumeratorValueResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorValueResponseModel>> UpdateValue(
            [FromRoute] Guid id,
            [FromRoute] Guid valueId,
            [FromBody] DataEnumeratorValueUpdateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var value = await _dataEnumeratorService.UpdateValueAsync(
                    valueId,
                    request.DisplayName,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status200OK, DataEnumeratorMapper.ToValueResponseModel(value));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator value not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update enumerator value {ValueId}", valueId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}/values/{valueId:guid}/active")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SetValueActive(
            [FromRoute] Guid id,
            [FromRoute] Guid valueId,
            [FromBody] DataEnumeratorSetActiveRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                await _dataEnumeratorService.SetValueActiveAsync(valueId, request.IsActive, user!, ct);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator value not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to set active state on enumerator value {ValueId}", valueId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}/values/reorder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReorderValues(
            [FromRoute] Guid id,
            [FromBody] ReorderValuesRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                await _dataEnumeratorService.ReorderValuesAsync(id, request.OrderedValueIds, user!, ct);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reorder values for enumerator {EnumeratorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        #endregion

        #region ========================= ENUMERATOR VALUE TRANSLATION ENDPOINTS =========================

        [HttpGet("{id:guid}/values/{valueId:guid}/translations")]
        [ProducesResponseType(typeof(List<DataEnumeratorValueTranslationResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DataEnumeratorValueTranslationResponseModel>>> GetTranslations(
            [FromRoute] Guid id,
            [FromRoute] Guid valueId,
            CancellationToken ct = default
        )
        {
            try
            {
                var (_, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var translations = await _dataEnumeratorService.GetTranslationsAsync(valueId, ct);

                var response = translations
                    .Select(DataEnumeratorMapper.ToTranslationResponseModel)
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get translations for value {ValueId}", valueId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("{id:guid}/values/{valueId:guid}/translations")]
        [ProducesResponseType(typeof(DataEnumeratorValueTranslationResponseModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorValueTranslationResponseModel>> CreateTranslation(
            [FromRoute] Guid id,
            [FromRoute] Guid valueId,
            [FromBody] DataEnumeratorValueTranslationCreateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var translation = await _dataEnumeratorService.CreateTranslationAsync(
                    valueId,
                    request.LanguageCode,
                    request.Translation,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status201Created, DataEnumeratorMapper.ToTranslationResponseModel(translation));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Enumerator value not found."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "A translation for that language already exists on this value."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create translation for value {ValueId}", valueId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpPatch("{id:guid}/values/{valueId:guid}/translations/{translationId:guid}")]
        [ProducesResponseType(typeof(DataEnumeratorValueTranslationResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorValueTranslationResponseModel>> UpdateTranslation(
            [FromRoute] Guid id,
            [FromRoute] Guid valueId,
            [FromRoute] Guid translationId,
            [FromBody] DataEnumeratorValueTranslationUpdateRequestModel request,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                var (user, error) = await GetCurrentUserAsync();
                if (error != null) return error;

                var translation = await _dataEnumeratorService.UpdateTranslationAsync(
                    translationId,
                    request.LanguageCode,
                    request.Translation,
                    user!,
                    ct
                );

                return StatusCode(StatusCodes.Status200OK, DataEnumeratorMapper.ToTranslationResponseModel(translation));
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Translation not found."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status409Conflict, new FailureResponseModel
                {
                    Detail = "A translation for that language already exists on this value."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update translation {TranslationId}", translationId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpDelete("{id:guid}/values/{valueId:guid}/translations/{translationId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteTranslation(
            [FromRoute] Guid id,
            [FromRoute] Guid valueId,
            [FromRoute] Guid translationId,
            CancellationToken ct = default
        )
        {
            try
            {
                var adminError = await RequireAdminAsync();
                if (adminError != null) return adminError;

                await _dataEnumeratorService.DeleteTranslationAsync(translationId, ct);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                {
                    Detail = "Translation not found."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete translation {TranslationId}", translationId);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        #endregion
    }
}
