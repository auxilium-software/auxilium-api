namespace AuxiliumAPI.Common.DataStructures.Internal
{
    public class CouchDBFindResponse<T>
    {
        public List<T>? Docs { get; set; }
        public string? Bookmark { get; set; }
        public string? Warning { get; set; }
    }
}
