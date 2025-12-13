namespace AuxiliumAPI.Common.Services.Interfaces
{
    public interface ICaptchaService
    {
        Task VerifyRecaptchaAsync(string token, string? clientIp);
    }
}
