namespace AuxiliumAPI.Common.EntityModels
{
    public class UserAdditionalProperty
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public Guid LastUpdatedBy { get; set; }



        public Guid UserId { get; set; }
        public string OriginalName { get; set; }
        public string PrettyName { get; set; }
        public string URLSlug { get; set; }
        public string ContentType { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
