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

        public async Task<bool> VerifyRecaptchaAsync(string token, string? clientIp)
        {
            try
            {

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", this._configuration["ReCAPTCHA:SecretKey"]!),
                    new KeyValuePair<string, string>("response", token),
                    new KeyValuePair<string, string>("remoteip", clientIp)
                });

                var response = await _httpClient.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify",
                    content
                );

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<RecaptchaResponse>();

                if (jsonResponse == null)
                {
                    _logger.LogError("Failed to parse reCAPTCHA response");
                    return false;
                }

                if (!jsonResponse.Success)
                {
                    _logger.LogWarning(
                        "reCAPTCHA verification failed. Errors: {Errors}",
                        string.Join(", ", jsonResponse.ErrorCodes ?? Array.Empty<string>())
                    );
                    return false;
                }

                if (jsonResponse.Score.HasValue && jsonResponse.Score < this._configuration!.GetValue<float>("ReCAPTCHA:ScoreThreshold"))
                {
                    _logger.LogWarning(
                        "reCAPTCHA score too low: {Score}",
                        jsonResponse.Score
                    );
                    return false;
                }

                _logger.LogInformation("reCAPTCHA verification successful");
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to contact reCAPTCHA service");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during reCAPTCHA verification");
                return false;
            }
        }
    }
}
