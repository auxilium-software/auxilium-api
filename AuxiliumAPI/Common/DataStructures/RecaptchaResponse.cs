namespace AuxiliumAPI.Common.DataStructures
{
    /// <summary>
    /// Represents the response from a reCAPTCHA verification request.
    /// </summary>
    internal class RecaptchaResponse
    {
        /**
         * Whether the reCAPTCHA verification was successful.
         */
        public bool Success { get; set; }

        /**
         * The timestamp of the challenge.
         */
        public DateTime ChallengeTs { get; set; }

        /**
         * The hostname of the site where the reCAPTCHA was solved.
         */
        public string? Hostname { get; set; }

        /**
         * The score assigned by reCAPTCHA (v3 only).
         */
        public double? Score { get; set; }

        /**
         * The action name for reCAPTCHA (v3 only).
         */
        public string? Action { get; set; }

        /**
         * Any error codes returned by the reCAPTCHA service.
         */
        public string[]? ErrorCodes { get; set; }
    }
}
