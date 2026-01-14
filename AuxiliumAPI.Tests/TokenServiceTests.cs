using AuxiliumSoftware.AuxiliumServices.Common.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;
using Xunit.Abstractions;



namespace AuxiliumAPI.Tests
{
    public class TokenServiceTests
    {
        private readonly ITestOutputHelper _output;

        private readonly TokenService _tokenService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Dictionary<string, object> _testUserData;

        public TokenServiceTests(
            ITestOutputHelper output
            )
        {
            _output = output;

            var inMemorySettings = new Dictionary<string, string>
            {
                {"JWT:SecretKey", "hunter2hunter2hunter2hunter2hunter2"},
                {"JWT:ValidIssuer", "Auxilium API"},
                {"JWT:ValidAudience", "Auxilium Portal"},
                {"JWT:AccessTokenExpireMinutes", "30"},
                {"JWT:RefreshTokenExpireDays", "7"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _tokenService = new TokenService(configuration);

            _testUserData = new Dictionary<string, object>
            {
                ["id"] = "12345678-1234-1234-1234-123456789876",
            };
        }


        #region CreateAccessToken Tests
        [Fact]
        public void CreateAccessToken_WithValidUserData_ReturnsJwtToken()
        {
            var token = _tokenService.CreateAccessToken(_testUserData);

            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Should().NotBeNull();
            jwtToken.Issuer.Should().Be("Auxilium API");
            jwtToken.Audiences.Should().Contain("Auxilium Portal");
        }

        [Fact]
        public void CreateAccessToken_ContainsSubClaim()
        {
            var token = _tokenService.CreateAccessToken(_testUserData);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            subClaim.Should().NotBeNull();
            subClaim!.Value.Should().Be("12345678-1234-1234-1234-123456789876");
        }

        [Fact]
        public void CreateAccessToken_ContainsJtiClaim()
        {
            var token = _tokenService.CreateAccessToken(_testUserData);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
            jtiClaim.Should().NotBeNull();
            Guid.TryParse(jtiClaim!.Value, out _).Should().BeTrue();
        }

        [Fact]
        public void CreateAccessToken_HasCorrectExpiration()
        {
            var token = _tokenService.CreateAccessToken(_testUserData);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
            jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void CreateAccessToken_GeneratesUniqueTokens()
        {
            var token1 = _tokenService.CreateAccessToken(_testUserData);
            var token2 = _tokenService.CreateAccessToken(_testUserData);

            token1.Should().NotBe(token2);
        }
        #endregion

        #region CreateRefreshToken Tests
        [Fact]
        public void CreateRefreshToken_WithValidUserData_ReturnsJwtToken()
        {
            var token = _tokenService.CreateRefreshToken(_testUserData);

            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Should().NotBeNull();
        }

        [Fact]
        public void CreateRefreshToken_HasLongerExpiration()
        {
            var refreshToken = _tokenService.CreateRefreshToken(_testUserData);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(refreshToken);

            var expectedExpiry = DateTime.UtcNow.AddDays(7);
            jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void CreateRefreshToken_ContainsSameClaims()
        {
            var accessToken = _tokenService.CreateAccessToken(_testUserData);
            var refreshToken = _tokenService.CreateRefreshToken(_testUserData);

            var handler = new JwtSecurityTokenHandler();
            var accessJwt = handler.ReadJwtToken(accessToken);
            var refreshJwt = handler.ReadJwtToken(refreshToken);

            var accessSub = accessJwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            var refreshSub = refreshJwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;

            accessSub.Should().Be(refreshSub);
        }
        #endregion
    }
}
