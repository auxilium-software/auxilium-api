namespace AuxiliumAPI.Common.EntityModels
{
    public class UserFileModel
    {
        /**
         * The unique identifier for the additional property.
         */
        public Guid Id { get; set; }
        /**
         * The timestamp when the additional property was created.
         */
        public DateTime CreatedAt { get; set; }
        /**
         * The unique identifier of the user who created the additional property.
         */
        public Guid CreatedBy { get; set; }
        /**
         * The timestamp when the additional property was last updated.
         */
        public DateTime LastUpdatedAt { get; set; }
        /**
         * The unique identifier of the user who last updated the additional property.
         */
        public Guid LastUpdatedBy { get; set; }



        /**
         * The unique identifier for the user this file is for
         */
        public Guid UserId { get; set; }
        /**
         * The original filename of the file.
         */
        public string Filename { get; set; }
        /**
         * The MIME type of the file (e.g., "image/png", "application/pdf").
         */
        public string ContentType { get; set; }
        /**
         * The size of the file in bytes.
         */
        public long Size { get; set; }
        /*
         * A hash (checksum) of the file for integrity verification.
         */
        public string Hash { get; set; }
        /**
         * The path (relative to that set in config) to the file in the LFS (Large File Storage) system.
         */
        public string LfsPath { get; set; }
        /**
         * An optional description of the file the user can set.
         */
        public string Description { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
