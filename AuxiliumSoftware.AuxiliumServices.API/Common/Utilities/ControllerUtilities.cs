using AuxiliumSoftware.AuxiliumServices.API.Models.Case;
using AuxiliumSoftware.AuxiliumServices.API.Models.User;
using AuxiliumSoftware.AuxiliumServices.Common.DataTransferObjects;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AuxiliumSoftware.AuxiliumServices.API.Common.Utilities
{
    public static class ControllerUtilities
    {
        public static IQueryable<UserEntityModel> ApplySortingForUsers(
            IQueryable<UserEntityModel> query,
            string? sortBy,
            string? sortOrder
        )
        {
            var descending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "createdat" => descending
                    ? query.OrderByDescending(u => u.CreatedAtUtc)
                    : query.OrderBy(u => u.CreatedAtUtc),
                "fullname" => descending
                    ? query.OrderByDescending(u => u.FullName)
                    : query.OrderBy(u => u.FullName),
                "email" => descending
                    ? query.OrderByDescending(u => u.EmailAddress)
                    : query.OrderBy(u => u.EmailAddress),
                _ => query.OrderByDescending(u => u.CreatedAtUtc)
            };
        }
        public static IQueryable<CaseEntityModel> ApplySortingForCases(
            IQueryable<CaseEntityModel> query,
            string? sortBy,
            string? sortOrder
        )
        {
            var descending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "createdat" => descending
                    ? query.OrderByDescending(c => c.CreatedAtUtc)
                    : query.OrderBy(c => c.CreatedAtUtc),
                "updatedat" => descending
                    ? query.OrderByDescending(c => c.LastUpdatedAtUtc)
                    : query.OrderBy(c => c.LastUpdatedAtUtc),
                "title" => descending
                    ? query.OrderByDescending(c => c.Title)
                    : query.OrderBy(c => c.Title),
                "status" => descending
                    ? query.OrderByDescending(c => c.Status)
                    : query.OrderBy(c => c.Status),
                _ => query.OrderByDescending(c => c.CreatedAtUtc)
            };
        }













        public static UserResponseModel UserMapToUserResponseModel(UserEntityModel userDoc, bool isAdmin)
        {
            if (isAdmin)
            {
                var additionalProperties = userDoc.AdditionalProperties?
                    .ToDictionary(
                        p => p.UrlSlug,
                        p => new AdditionalPropertySubStructureDTO
                        {
                            Id = p.Id,
                            CreatedAt = p.CreatedAtUtc,
                            CreatedBy = p.CreatedByUserId,
                            UpdatedAt = p.LastUpdatedAtUtc,
                            LastUpdatedBy = p.LastUpdatedByUserId,
                            OriginalName = p.OriginalName,
                            UrlSlug = p.UrlSlug,
                            Content = p.Content,
                            ContentType = p.ContentType
                        }
                    ) ?? new Dictionary<string, AdditionalPropertySubStructureDTO>();

                return new UserResponseModel
                {
                    ID = userDoc.Id,
                    CreatedAt = userDoc.CreatedAtUtc,
                    CreatedBy = userDoc.CreatedByUserId,
                    LastUpdatedAt = userDoc.LastUpdatedAtUtc,
                    LastUpdatedBy = userDoc.LastUpdatedByUserId,

                    EmailAddress = userDoc.EmailAddress,
                    FullName = userDoc.FullName ?? string.Empty,
                    FullAddress = userDoc.FullAddress ?? string.Empty,
                    TelephoneNumber = userDoc.TelephoneNumber ?? string.Empty,
                    Gender = userDoc.Gender ?? string.Empty,
                    DateOfBirth = userDoc.DateOfBirth,
                    LanguagePreference = userDoc.LanguagePreference,

                    AdditionalProperties = additionalProperties,
                    Files = userDoc.Files?.Select(f => $"auxlfs://localhost/user-file/{f.Id}").ToList() ?? new List<string>(),

                    HowDidYouFindOutAboutOurService = userDoc.HowDidYouFindOutAboutOurService ?? string.Empty,

                    IsEmailVerified = userDoc.HasEmailAddressBeenVerified,
                    AllowLogin = userDoc.AllowLogin,
                    IsAdministrator = userDoc.IsAdministrator,
                    IsCaseWorkerManager = userDoc.IsCaseWorkerManager,
                    IsCaseWorker = userDoc.IsCaseWorker
                };
            }

            return new UserResponseModel
            {
                ID = userDoc.Id,
                CreatedAt = userDoc.CreatedAtUtc,
                CreatedBy = userDoc.CreatedByUserId,
                LastUpdatedAt = null,
                LastUpdatedBy = null,

                EmailAddress = "[REDACTED]",
                FullName = userDoc.FullName ?? string.Empty,
                FullAddress = "[REDACTED]",
                TelephoneNumber = "[REDACTED]",
                Gender = "[REDACTED]",
                DateOfBirth = null,
                LanguagePreference = "[REDACTED]",

                AdditionalProperties = new Dictionary<string, AdditionalPropertySubStructureDTO>(),
                Files = new List<string>(),

                HowDidYouFindOutAboutOurService = "[REDACTED]",

                IsEmailVerified = null,
                AllowLogin = null,
                IsAdministrator = null,
                IsCaseWorkerManager = null,
                IsCaseWorker = null
            };
        }
        public static CaseResponseModel CaseMapToCaseResponseModel(CaseEntityModel caseEntity)
        {
            return new CaseResponseModel
            {
                ID = caseEntity.Id,
                CreatedAt = caseEntity.CreatedAtUtc,
                CreatedBy = caseEntity.CreatedByUserId,
                LastUpdatedAt = caseEntity.LastUpdatedAtUtc,
                LastUpdatedBy = caseEntity.LastUpdatedByUserId,

                Title = caseEntity.Title,
                Description = caseEntity.Description,
                Status = caseEntity.Status,
                Sensitivity = caseEntity.Sensitivity,

                Clients = caseEntity.Clients?.Select(c => c.UserId).ToList() ?? new List<Guid>(),
                Workers = caseEntity.Workers?.Select(w => w.UserId).ToList() ?? new List<Guid>(),

                Files = caseEntity.Files?.Select(f => $"auxlfs://localhost/case-file/{f.Id}").ToList() ?? new List<string>(),
                Messages = caseEntity.Messages?.Select(m => $"auxmsg://localhost/message/{m.Id}").ToList() ?? new List<string>(),

                Referrer = null,

                // Map todos from entities
                Todos = caseEntity.Todos?
                    .ToDictionary(
                        t => t.Id.ToString(),
                        t => (object)new
                        {
                            id = t.Id,
                            summary = t.Summary,
                            description = t.Description,
                            status = t.Status.ToString(),
                            priority = t.Priority.ToString(),
                            due_date = t.DueDate,
                            assigned_to = t.AssignedToUserId,
                            completed_at = t.CompletedAtUtc
                        }
                    ) ?? new Dictionary<string, object>(),
                /*
                Timeline = caseEntity.Timeline?
                    .ToDictionary(
                        t => t.Id.ToString(),
                        t => (object)new
                        {
                            id = t.Id,
                        }
                    ) ?? new Dictionary<string, object>(),
                */

                AdditionalProperties = caseEntity.AdditionalProperties?
                    .ToDictionary(
                        p => p.UrlSlug,
                        p => new AdditionalPropertySubStructureDTO
                        {
                            Id = p.Id,
                            CreatedAt = p.CreatedAtUtc,
                            CreatedBy = p.CreatedByUserId,
                            UpdatedAt = p.LastUpdatedAtUtc,
                            LastUpdatedBy = p.LastUpdatedByUserId,
                            OriginalName = p.OriginalName,
                            UrlSlug = p.UrlSlug,
                            Content = p.Content,
                            ContentType = p.ContentType
                        }
                    ) ?? new Dictionary<string, AdditionalPropertySubStructureDTO>(),
            };
        }





















        public static string? SanitisePropertyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var sanitized = name
                .Trim()
                .ToLowerInvariant()
                .Replace(' ', '_');

            // only allow alphanumeric, underscore, hyphen
            if (!System.Text.RegularExpressions.Regex.IsMatch(sanitized, @"^[a-z0-9_-]+$"))
                return null;

            return sanitized.Length > 100 ? sanitized[..100] : sanitized;
        }



















        public static async Task<bool> HasUserGotConnectionToUser(AuxiliumDbContext Db, UserEntityModel yourself, UserEntityModel targetUser)
        {
            if (yourself.IsAdministrator)
                return true;

            if (yourself.Id == targetUser.Id)
                return true;

            if (yourself.IsCaseWorker)
            {
                // case workers can see clients on cases they are assigned to
                return await Db.CaseWorkers.AnyAsync(cw =>
                    cw.UserId == yourself.Id
                    && (
                        Db.CaseClients.Any(cc => cc.CaseId == cw.CaseId && cc.UserId == targetUser.Id)
                        || Db.CaseWorkers.Any(cw2 => cw2.CaseId == cw.CaseId && cw2.UserId == targetUser.Id)
                    )
                );
            }

            // non-admins see users they share cases with (as co-clients or co-workers)
            return await Db.CaseClients.AnyAsync(cc =>
                cc.UserId == yourself.Id
                && (
                    Db.CaseClients.Any(cc2 => cc2.CaseId == cc.CaseId && cc2.UserId == targetUser.Id)
                    || Db.CaseWorkers.Any(cw => cw.CaseId == cc.CaseId && cw.UserId == targetUser.Id)
                )
            );
        }

        public static async Task<bool> CanUserModifyUser(AuxiliumDbContext Db, UserEntityModel yourself, UserEntityModel targetUser)
        {
            if (yourself.IsAdministrator)
                return true;

            if (yourself.Id == targetUser.Id)
                return true;

            if (yourself.IsCaseWorker)
            {
                // case workers can modify clients on their assigned cases
                return await Db.CaseWorkers.AnyAsync(cw =>
                    cw.UserId == yourself.Id
                    && Db.CaseClients.Any(cc => cc.CaseId == cw.CaseId && cc.UserId == targetUser.Id)
                );
            }

            // regular users cannot modify other users
            return false;
        }














        public static async Task EnrichEnumPropertiesAsync(
            IDictionary<string, AdditionalPropertySubStructureDTO> properties,
            IDataEnumeratorService dataEnumeratorService,
            string? locale,
            CancellationToken ct = default)
        {
            if (properties == null || properties.Count == 0) return;

            var valueIds = new List<Guid>();
            foreach (var p in properties.Values)
            {
                if (p.ContentType == Common.Constants.DataEnumeratorReferenceContentType
                    && Guid.TryParse(p.Content, out var vid))
                {
                    valueIds.Add(vid);
                }
            }

            if (valueIds.Count == 0) return;

            var resolved = await dataEnumeratorService.ResolveValueDisplaysAsync(valueIds, locale, ct);

            foreach (var p in properties.Values)
            {
                if (p.ContentType == Common.Constants.DataEnumeratorReferenceContentType
                    && Guid.TryParse(p.Content, out var vid)
                    && resolved.TryGetValue(vid, out var r))
                {
                    p.DataEnumeratorId = r.EnumTypeId;
                    p.DisplayValue = r.ValueDisplay;
                    p.DataEnumeratorDisplayName = r.EnumDisplay;
                }
            }
        }
    }
}
