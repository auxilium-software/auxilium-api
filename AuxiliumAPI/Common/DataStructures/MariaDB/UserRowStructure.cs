namespace AuxiliumAPI.Common.DataStructures.MariaDB
{
    public class UserRowStructure
    {
        public required Guid id { get; set; }
        public DateTime created_at { get; set; }
        public required string email_address { get; set; }
        public required string password_hash { get; set; }
        public bool allow_login { get; set; } = false;
        public bool is_admin { get; set; } = false;
    }
}
