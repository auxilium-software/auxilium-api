using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.File
{
    public class FileUploadRequestModel
    {
        [Required]
        [JsonPropertyName("file")]
        public required IFormFile File { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; } = null;
    }
}
