using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValue;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValueTranslation;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework.EntityModels;

namespace AuxiliumSoftware.AuxiliumServices.API.Mappers
{
    public static class DataEnumeratorMapper
    {
        public static DataEnumeratorResponseModel ToResponseModel(
            DataEnumeratorEntityModel entity,
            string? locale = null)
        {
            return new DataEnumeratorResponseModel
            {
                Id = entity.Id,
                CanonicalName = entity.CanonicalName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAtUtc = entity.CreatedAtUtc,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc,
                Values = entity.EnumeratorValues?
                    .Select(v => ToValueResponseModel(v, locale))
                    .ToList()
            };
        }

        public static DataEnumeratorValueResponseModel ToValueResponseModel(
            DataEnumeratorValueEntityModel entity,
            string? locale = null)
        {

            return new DataEnumeratorValueResponseModel
            {
                Id = entity.Id,
                EnumTypeId = entity.EnumTypeId,
                CanonicalName = entity.CanonicalName,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                CreatedAtUtc = entity.CreatedAtUtc,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc,
            };
        }

        public static DataEnumeratorValueTranslationResponseModel ToTranslationResponseModel(
            DataEnumeratorValueTranslationEntityModel entity)
        {
            return new DataEnumeratorValueTranslationResponseModel
            {
                Id = entity.Id,
                DataEnumeratorValueId = entity.DataEnumeratorValueId,
                LanguageCode = entity.LanguageCode,
                Translation = entity.Translation,
                CreatedAtUtc = entity.CreatedAtUtc,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc,
            };
        }
    }
}
