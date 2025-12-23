namespace AuxiliumAPI.Common.DataStructures
{
    internal class RecaptchaResponse
    {
        public bool Success { get; set; }

        public DateTime ChallengeTs { get; set; }

        public string? Hostname { get; set; }

        public double? Score { get; set; }

        public string? Action { get; set; }

        public string[]? ErrorCodes { get; set; }
    }
}
