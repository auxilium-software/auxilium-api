using AuxiliumAPI.Common.DataStructures.Internal;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;

namespace AuxiliumAPI.Common.Services
{
    public class CaptchaService : ICaptchaService
    {
        private readonly HttpClient _httpClient;

        public CaptchaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task VerifyRecaptchaAsync(string token, string? clientIp)
        {
            var parameters = new Dictionary<string, string>
            {
                ["secret"]   = ConfigurationUtilities.GetString("ReCAPTCHA", "SecretKey"),
                ["response"] = token
            };

            if (!string.IsNullOrEmpty(clientIp))
            {
                parameters["remoteip"] = clientIp;
            }

            var response = await _httpClient.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(parameters)
            );

            var jsonResponse = await response.Content.ReadFromJsonAsync<RecaptchaResponse>();

            if (jsonResponse?.Success != true)
            {
                throw new HttpRequestException("reCAPTCHA verification failed");
            }
        }
    }
}
