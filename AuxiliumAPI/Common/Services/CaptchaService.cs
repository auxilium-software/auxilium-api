using AuxiliumAPI.Common.DataStructures.Internal;
using AuxiliumAPI.Common.Services.Interfaces;

namespace AuxiliumAPI.Common.Services
{
    public class CaptchaService : ICaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CaptchaService> _logger;
        private readonly HttpClient _httpClient;

        public CaptchaService(
            IConfiguration configuration,
            ILogger<CaptchaService> logger,
            HttpClient httpClient
            )
        {
            this._configuration = configuration;
            this._logger = logger;
            this._httpClient = httpClient;
        }

        public async Task VerifyRecaptchaAsync(string token, string? clientIp)
        {
            var parameters = new Dictionary<string, string>
            {
                ["secret"]   = this._configuration["ReCAPTCHA:SecretKey"]!,
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
