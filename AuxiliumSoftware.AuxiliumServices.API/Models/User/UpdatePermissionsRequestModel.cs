namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class UpdatePermissionsRequestModel
    {
        public bool? IsAdmin { get; set; }
        public bool? IsCaseWorker { get; set; }
    }
}
