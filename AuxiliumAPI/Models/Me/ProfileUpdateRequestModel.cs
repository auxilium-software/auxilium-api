namespace AuxiliumAPI.Models.Me
{
    public class ProfileUpdateRequestModel
    {
        public string? FullName { get; set; }
        public string? FullAddress { get; set; }
        public string? TelephoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? HowDidYouFindOutAboutOurService { get; set; }
    }
}
