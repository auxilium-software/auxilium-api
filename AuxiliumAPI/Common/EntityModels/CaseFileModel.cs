namespace AuxiliumAPI.Common.EntityModels
{
    public class CaseFileModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public Guid LastUpdatedBy { get; set; }



        public Guid CaseId { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
        public string LfsPath { get; set; }
        public string Description { get; set; }



        public UserModel CreatedByUser { get; set; }
        public UserModel LastUpdatedByUser { get; set; }
        public CaseModel Case { get; set; }
    }
}
