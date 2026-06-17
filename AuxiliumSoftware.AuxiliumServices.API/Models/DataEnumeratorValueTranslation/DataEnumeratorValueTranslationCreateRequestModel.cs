namespace AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumeratorValueTranslation
{
    public class DataEnumeratorValueTranslationCreateRequestModel
    {
        public required string LanguageCode { get; set; }
        public required string Translation { get; set; }
    }
}
