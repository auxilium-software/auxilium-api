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
            return Ok(settings);
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
                return NotFound();

            return Ok(setting);
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(AllSystemSettingsResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AllSystemSettingsResponseModel>> GetAllSettings()
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;
            await this.RequireAdminAsync();

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
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                object? currentValue;
                bool isDefault;
                DateTime? lastModified = null;
                Guid? lastModifiedBy = null;

                if (dbSetting is not null)
                {
                    currentValue = DeserializeValue(dbSetting.ConfigValue, typeAttr?.ValueType ?? SystemSettingValueTypeEnum.String);
                    isDefault = false;
                    lastModified = dbSetting.CreatedAt;
                    lastModifiedBy = dbSetting.CreatedBy;
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

            return Ok(new AllSystemSettingsResponseModel
            {
                Settings = settings,
                TotalCount = settings.Count
            });
        }

        [HttpGet("{settingKey}")]
        [ProducesResponseType(typeof(SystemSettingResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SystemSettingResponseModel>> GetSetting(
            SystemSettingKeyEnum settingKey
        )
        {
            var (user, error) = await GetCurrentUserAsync();
            if (error != null) return error;

            await this.RequireAdminAsync();

            var setting = await this.Db.System_Settings
                .AsNoTracking()
                .Where(s => s.ConfigKey == settingKey)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync();

            if (setting is null)
                return NotFound(new { message = $"Setting not found: {settingKey}" });

            return Ok(new SystemSettingResponseModel
            {
                Key = setting.ConfigKey,
                Value = setting.ConfigValue,
                ValueType = setting.ValueType,
                ModifiedAt = setting.CreatedAt,
                ModifiedBy = setting.CreatedBy,
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

            await this.RequireAdminAsync();

            var history = await this.Db.System_Settings
                .AsNoTracking()
                .Where(s => s.ConfigKey == settingKey)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.CreatedByUser)
                .ToListAsync();

            return Ok(history.Select(s => new SystemSettingHistoryResponseModel
            {
                Value = s.ConfigValue,
                ValueType = s.ValueType,
                ModifiedAt = s.CreatedAt,
                ModifiedBy = s.CreatedBy,
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

            await this.RequireAdminAsync();

            await this.SystemSettings.SetAsync(
                settingKey,
                request.Value,
                user!.Id,
                request.ReasonForModification
            );

            var setting = await this.Db.System_Settings
                .AsNoTracking()
                .Where(s => s.ConfigKey == settingKey)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.CreatedByUser)
                .FirstAsync();

            return Ok(new SystemSettingResponseModel
            {
                Key = setting.ConfigKey,
                Value = setting.ConfigValue,
                ValueType = setting.ValueType,
                ModifiedAt = setting.CreatedAt,
                ModifiedBy = setting.CreatedBy,
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
            return SystemSettingVisibilityEnum.Public
        }

        private static object? GetDefaultValue(
            SystemSettingDefaultValueAttribute? defaultAttr,
            SystemSettingValueTypeEnum? valueType)
        {
            if (defaultAttr?.DefaultValue is null)
                return null;

            if (valueType == SystemSettingValueTypeEnum.StringArray)
            {
                var stringValue = defaultAttr.DefaultValue.ToString() ?? "";
                return string.IsNullOrEmpty(stringValue)
                    ? new List<string>()
                    : stringValue.Split(',').Select(s => s.Trim()).ToList();
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
                SystemSettingValueTypeEnum.StringArray => JsonSerializer.Deserialize<List<string>>(json),
                SystemSettingValueTypeEnum.Json => JsonSerializer.Deserialize<JsonElement>(json),
                _ => json
            };
        }
    }
}
