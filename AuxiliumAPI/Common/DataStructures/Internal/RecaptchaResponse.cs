namespace AuxiliumAPI.Common.DataStructures.Internal
{
    internal class RecaptchaResponse
    {
        public bool Success { get; set; }
        public string? ChallengeTs { get; set; }
        public string? Hostname { get; set; }
    }
}
