using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuxiliumAPI.Common.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration Configuration;
        public TokenService(
            IConfiguration configuration
            )
        {
            this.Configuration = configuration;
        }

        public string CreateAccessToken(Dictionary<string, object> userData)
        {
            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(this.Configuration["JWT:SecretKey"]!));
            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

            List<Claim> claims =
            [
                new(JwtRegisteredClaimNames.Sub, userData["id"].ToString()!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ];

            JwtSecurityToken token = new(
                issuer: this.Configuration["JWT:ValidIssuer"],
                audience: this.Configuration["JWT:ValidAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(this.Configuration.GetValue<int>("JWT:AccessTokenExpireMinutes")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefreshToken(Dictionary<string, object> userData)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.Configuration["JWT:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userData["id"].ToString()!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: this.Configuration["JWT:ValidIssuer"],
                audience: this.Configuration["JWT:ValidAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(this.Configuration.GetValue<int>("JWT:RefreshTokenExpireDays")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
