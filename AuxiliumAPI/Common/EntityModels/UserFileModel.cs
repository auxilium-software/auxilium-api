namespace AuxiliumAPI.Common.EntityModels
{
    public class UserFileModel
    {
        /// <summary>
        /// The unique identifier for the additional property.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The timestamp when the additional property was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who created the additional property.
        /// </summary>
        public Guid CreatedBy { get; set; }
        /// <summary>
        /// The timestamp when the additional property was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }
        /// <summary>
        /// The unique identifier of the user who last updated the additional property.
        /// </summary>
        public Guid LastUpdatedBy { get; set; }



        /// <summary>
        /// The unique identifier for the user this file is for.
        /// </summary>
        public Guid UserId { get; set; }
        /// <summary>
        /// The original filename of the file.
        /// </summary>
        public string Filename { get; set; }
        /// <summary>
        /// The MIME type of the file (e.g., "image/png", "application/pdf").
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// A hash (checksum) of the file for integrity verification.
        /// </summary>
        public string Hash { get; set; }
        /// <summary>
        /// The path (relative to that set in config) to the file in the LFS (Large File Storage) system.
        /// </summary>
        public string LfsPath { get; set; }
        /// <summary>
        /// An optional description of the file the user can set.
        /// </summary>
        public string Description { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public UserModel User { get; set; }
    }
}
