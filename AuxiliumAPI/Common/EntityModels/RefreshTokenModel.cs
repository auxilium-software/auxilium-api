namespace AuxiliumAPI.Common.EntityModels
{
    public class RefreshTokenModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }



        public string TokenHash { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
