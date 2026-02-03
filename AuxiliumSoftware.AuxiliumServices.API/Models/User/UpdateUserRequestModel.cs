using System.ComponentModel.DataAnnotations;

namespace AuxiliumSoftware.AuxiliumServices.API.Models.User
{
    public class UpdateUserRequestModel
    {
        public string? FullName { get; set; }


        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? EmailAddress { get; set; }


        [Phone(ErrorMessage = "Invalid telephone number")]
        public string? TelephoneNumber { get; set; }


        public string? FullAddress { get; set; }


        public string? Gender { get; set; }


        public DateOnly? DateOfBirth { get; set; }


        public string? LanguagePreference { get; set; }


        public string? HowDidYouFindOutAboutOurService { get; set; }
    }
}
