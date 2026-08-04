using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class UpdatePermissionsRequestModel
    {
        [JsonPropertyName("isAdministrator")]
        public bool? IsAdministrator { get; set; }


        [JsonPropertyName("isCaseWorker")]
        public bool? IsCaseWorker { get; set; }


        [JsonPropertyName("isCaseWorkerManager")]
        public bool? IsCaseWorkerManager { get; set; }
    }
}
