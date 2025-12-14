namespace AuxiliumAPI.Common.DataStructures.Internal
{
    public class CouchDBQueryResult<T>
    {
        public List<T> Documents { get; set; } = new();
        public string? Bookmark { get; set; }
        public string? Warning { get; set; }
    }
}
