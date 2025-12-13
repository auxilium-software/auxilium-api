using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumAPI.Models.File
{
    public class FileUploadRequestModel
    {
        [Required]
        [JsonPropertyName("file")]
        public required IFormFile File { get; set; }

        [Required]
        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [Required]
        [JsonPropertyName("userId")]
        public required string UserId { get; set; }
    }
}
