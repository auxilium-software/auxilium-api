namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class UpdatePermissionsRequestModel
    {
        public bool? IsAdministrator { get; set; }
        public bool? IsCaseWorker { get; set; }
    }
}
