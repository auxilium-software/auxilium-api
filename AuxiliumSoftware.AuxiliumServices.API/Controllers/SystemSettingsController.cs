using AuxiliumSoftware.AuxiliumServices.API.Common.ControllerBases;
using AuxiliumSoftware.AuxiliumServices.API.Models.SystemSettings;
using AuxiliumSoftware.AuxiliumServices.Common.Attributes;
using AuxiliumSoftware.AuxiliumServices.Common.DataTransferObjects;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/system-settings")]
    [Tags("System Settings")]
    public class SystemSettingsController : LoggedInControllerBase
    {
        private static readonly Dictionary<string, SystemSettingKeyEnum> _jsonKeyToEnum = BuildJsonKeyLookup();

        private static Dictionary<string, SystemSettingKeyEnum> BuildJsonKeyLookup()
        {
            var map = new Dictionary<string, SystemSettingKeyEnum>(StringComparer.OrdinalIgnoreCase);

            foreach (SystemSettingKeyEnum key in Enum.GetValues<SystemSettingKeyEnum>())
            {
                var field = typeof(SystemSettingKeyEnum).GetField(key.ToString());
                var jsonNameAttr = field?.GetCustomAttribute<JsonPropertyNameAttribute>();

                if (jsonNameAttr is not null)
                    map[jsonNameAttr.Name] = key;
            }

            return map;
        }

        private static bool TryResolveSettingKey(string jsonKey, out SystemSettingKeyEnum settingKey) => _jsonKeyToEnum.TryGetValue(jsonKey, out settingKey);


        public SystemSettingsController(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            AuxiliumDbContext db,
            IWebApplicationFirewallService waf,
            ILogger<SystemSettingsController> logger,
            ITotpService totpService
        )
            : base(systemSettingsService, configuration, db, waf, logger, totpService)
        {
        }

        /// <summary>
        /// Returns all system settings the caller is entitled to see based on their authentication level.
        /// Unauthenticated callers receive only Public settings; authenticated non-admins receive Public + Authenticated; admins receive everything.
        /// </summary>
        [HttpGet("visible")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SystemSettingDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVisibleSettings(CancellationToken ct)
        {
            var visibility = ResolveCallerVisibility();
            var settings = await this.SystemSettings.GetVisibleSettingsAsync(visibility, ct);
            return StatusCode(StatusCodes.Status200OK, settings);
        }

        /// <summary>
        /// Returns a single system setting by its JSON key name (e.g. "instance.branding.name"), provided the caller has sufficient visibility.
        /// Returns 404 for missing keys AND for keys above the caller's visibility tier to avoid leaking key existence.
        /// </summary>
        [HttpGet("visible/{*jsonKey}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SystemSettingDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVisibleSetting(string jsonKey, CancellationToken ct)
        {
            var visibility = ResolveCallerVisibility();
            var setting = await this.SystemSettings.GetVisibleSettingByKeyAsync(jsonKey, visibility, ct);

            if (setting is null)
                return StatusCode(StatusCodes.Status404NotFound);

            return StatusCode(StatusCodes.Status200OK, setting);
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(AllSystemSettingsResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AllSystemSettingsResponseModel>> GetAllSettings()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var settings = new List<SystemSettingItemResponseModel>();

            foreach (SystemSettingKeyEnum key in Enum.GetValues<SystemSettingKeyEnum>())
            {
                var field = typeof(SystemSettingKeyEnum).GetField(key.ToString());

                var jsonNameAttr = field?.GetCustomAttribute<JsonPropertyNameAttribute>();
                var typeAttr = field?.GetCustomAttribute<SystemSettingExpectedValueTypeAttribute>();
                var defaultAttr = field?.GetCustomAttribute<SystemSettingDefaultValueAttribute>();
                var descriptionAttr = field?.GetCustomAttribute<SystemSettingDescriptionAttribute>();
                var recommendationAttr = field?.GetCustomAttribute<SystemSettingRecommendationAttribute>();

                var dbSetting = await this.Db.System_Settings
                    .AsNoTracking()
                    .Where(s => s.ConfigKey == key)
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .FirstOrDefaultAsync();

                object? currentValue;
                bool isDefault;
                DateTime? lastModified = null;
                Guid? lastModifiedBy = null;

                if (dbSetting is not null)
                {
                    currentValue = DeserializeValue(dbSetting.ConfigValue, typeAttr?.ValueType ?? SystemSettingValueTypeEnum.String);
                    isDefault = false;
                    lastModified = dbSetting.CreatedAtUtc;
                    lastModifiedBy = dbSetting.CreatedByUserId;
                }
                else
                {
                    currentValue = GetDefaultValue(defaultAttr, typeAttr?.ValueType);
                    isDefault = true;
                }

                settings.Add(new SystemSettingItemResponseModel
                {
                    Key = key.ToString(),
                    JsonKey = jsonNameAttr?.Name ?? key.ToString(),
                    Value = currentValue,
                    ValueType = typeAttr?.ValueType.ToString() ?? "Unknown",
                    DefaultValue = GetDefaultValue(defaultAttr, typeAttr?.ValueType),
                    Description = descriptionAttr.Description,
                    Recommendation = recommendationAttr.Recommendation,
                    IsUsingDefault = isDefault,
                    LastModifiedAt = lastModified,
                    LastModifiedBy = lastModifiedBy
                });
            }

            return StatusCode(StatusCodes.Status200OK, new AllSystemSettingsResponseModel
            {
                Settings = settings,
                TotalCount = settings.Count
            });
        }

        [HttpGet("{settingKey}")]
        [ProducesResponseType(typeof(SystemSettingResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SystemSettingResponseModel>> GetSetting(string settingKey)
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            if (!TryResolveSettingKey(settingKey, out var resolvedKey))
                return StatusCode(StatusCodes.Status404NotFound, new { message = $"Setting not found: {settingKey}" });

            var setting = await this.Db.System_Settings
                .AsNoTracking()
                .Where(s => s.ConfigKey == resolvedKey)
                .OrderByDescending(s => s.CreatedAtUtc)
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync();

            if (setting is null)
                return StatusCode(StatusCodes.Status404NotFound, new { message = $"Setting not found: {settingKey}" });

            return StatusCode(StatusCodes.Status200OK, new SystemSettingResponseModel
            {
                Key = setting.ConfigKey,
                Value = setting.ConfigValue,
                ValueType = setting.ValueType,
                ModifiedAt = setting.CreatedAtUtc,
                ModifiedBy = setting.CreatedByUserId,
                ModifiedByName = setting.CreatedByUser?.FullName,
                ReasonForModification = setting.ReasonForModification
            });
        }

        [HttpGet("{settingKey}/history")]
        [ProducesResponseType(typeof(List<SystemSettingHistoryResponseModel>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SystemSettingHistoryResponseModel>>> GetSettingHistory(
            SystemSettingKeyEnum settingKey
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            var history = await this.Db.System_Settings
                .AsNoTracking()
                .Where(s => s.ConfigKey == settingKey)
                .OrderByDescending(s => s.CreatedAtUtc)
                .Include(s => s.CreatedByUser)
                .ToListAsync();

            return StatusCode(StatusCodes.Status200OK, history.Select(s => new SystemSettingHistoryResponseModel
            {
                Value = s.ConfigValue,
                ValueType = s.ValueType,
                ModifiedAt = s.CreatedAtUtc,
                ModifiedBy = s.CreatedByUserId,
                ModifiedByName = s.CreatedByUser?.FullName,
                ReasonForModification = s.ReasonForModification
            }).ToList());
        }

        [HttpPut("{settingKey}")]
        [ProducesResponseType(typeof(SystemSettingResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SystemSettingResponseModel>> SetSetting(
            SystemSettingKeyEnum settingKey,
            [FromBody] SystemSettingUpdateRequestModel request
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            var adminError = await this.RequireAdminAsync();
            if (adminError != null) return adminError;

            await this.SystemSettings.SetAsync(
                settingKey,
                request.Value,
                user!.Id,
                request.ReasonForModification
            );

            var setting = await this.Db.System_Settings
                .AsNoTracking()
                .Where(s => s.ConfigKey == settingKey)
                .OrderByDescending(s => s.CreatedAtUtc)
                .Include(s => s.CreatedByUser)
                .FirstAsync();

            return StatusCode(StatusCodes.Status200OK, new SystemSettingResponseModel
            {
                Key = setting.ConfigKey,
                Value = setting.ConfigValue,
                ValueType = setting.ValueType,
                ModifiedAt = setting.CreatedAtUtc,
                ModifiedBy = setting.CreatedByUserId,
                ModifiedByName = setting.CreatedByUser?.FullName,
                ReasonForModification = setting.ReasonForModification
            });
        }

        /// <summary>
        /// Determines the maximum visibility tier the current caller is entitled to.
        /// Unlike GetCurrentUserAsync(), this does not fail for unauthenticated requests.
        /// </summary>
        private SystemSettingVisibilityEnum ResolveCallerVisibility()
        {
            // if (!User.Identity?.IsAuthenticated ?? true)
            //     return SystemSettingVisibilityEnum.Public;

            if (User.IsInRole("Administrator"))
                return SystemSettingVisibilityEnum.Administrator;

            // return SystemSettingVisibilityEnum.Authenticated;
            return SystemSettingVisibilityEnum.Public;
        }

        private static object? GetDefaultValue(
            SystemSettingDefaultValueAttribute? defaultAttr,
            SystemSettingValueTypeEnum? valueType)
        {
            if (defaultAttr?.DefaultValue is null)
                return null;

            if (valueType == SystemSettingValueTypeEnum.DayArray)
            {
                var stringValue = defaultAttr.DefaultValue.ToString() ?? "";
                return string.IsNullOrEmpty(stringValue)
                    ? new List<DayOfWeek>()
                    : stringValue.Split(',').Select(s => Enum.Parse<DayOfWeek>(s.Trim())).ToList();
            }

            return defaultAttr.DefaultValue;
        }

        private static object? DeserializeValue(string json, SystemSettingValueTypeEnum valueType)
        {
            return valueType switch
            {
                SystemSettingValueTypeEnum.String => JsonSerializer.Deserialize<string>(json),
                SystemSettingValueTypeEnum.Int => JsonSerializer.Deserialize<int>(json),
                SystemSettingValueTypeEnum.Bool => JsonSerializer.Deserialize<bool>(json),
                SystemSettingValueTypeEnum.Decimal => JsonSerializer.Deserialize<decimal>(json),
                SystemSettingValueTypeEnum.DayArray => JsonSerializer.Deserialize<List<DayOfWeek>>(json),
                // SystemSettingValueTypeEnum.StringArray => JsonSerializer.Deserialize<List<string>>(json),
                // SystemSettingValueTypeEnum.Json => JsonSerializer.Deserialize<JsonElement>(json),
                _ => json
            };
        }
    }
}
