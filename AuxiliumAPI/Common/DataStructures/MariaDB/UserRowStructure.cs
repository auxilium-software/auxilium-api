namespace AuxiliumAPI.Common.DataStructures.MariaDB
{
    public class UserRowStructure
    {
        public required Guid id { get; set; }
        public required DateTime created_at { get; set; }
        public required string email_address { get; set; }
        public required string password_hash { get; set; }
        public required bool allow_login { get; set; } = false;
        public required bool is_admin { get; set; } = false;
        public required bool is_case_worker { get; set; } = false;
    }
}
