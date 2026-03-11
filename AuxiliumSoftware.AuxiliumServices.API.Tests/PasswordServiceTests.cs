using Xunit;
using FluentAssertions;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Implementations;
using Microsoft.Extensions.Configuration;

namespace AuxiliumSoftware.AuxiliumServices.API.Tests;

public class PasswordServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService(_configuration);
    }

    #region Argon2 Tests
    [Fact]
    public void VerifyPassword_WithKnownArgon2Hash_ShouldSucceed()
    {
        var password = "hunter2";
        var knownHash = "$argon2id$v=19$m=65536,t=3,p=1$CWGMkdKacy7FuDcGoJQS4g$mJFT0pW3Hw+tLcdAnW4aEmlTn7x7ChZIwf/gZkf8E+0";

        var result = _passwordService.VerifyPassword(password, knownHash);

        result.Should().BeTrue("the password 'hunter2' should verify against its known hash");
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldFail()
    {
        var wrongPassword = "hunter3";
        var hash = "$argon2id$v=19$m=65536,t=3,p=1$CWGMkdKacy7FuDcGoJQS4g$mJFT0pW3Hw+tLcdAnW4aEmlTn7x7ChZIwf/gZkf8E+0";

        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        result.Should().BeFalse("wrong password should not verify");
    }

    [Fact]
    public void VerifyPassword_WithTrailingSpace_ShouldFail()
    {
        var passwordWithSpace = "hunter2 ";
        var hash = "$argon2id$v=19$m=65536,t=3,p=1$CWGMkdKacy7FuDcGoJQS4g$mJFT0pW3Hw+tLcdAnW4aEmlTn7x7ChZIwf/gZkf8E+0";

        var result = _passwordService.VerifyPassword(passwordWithSpace, hash);

        result.Should().BeFalse("password with trailing space should not match");
    }

    [Fact]
    public void VerifyPassword_WithLeadingSpace_ShouldFail()
    {
        var passwordWithSpace = " hunter2";
        var hash = "$argon2id$v=19$m=65536,t=3,p=1$CWGMkdKacy7FuDcGoJQS4g$mJFT0pW3Hw+tLcdAnW4aEmlTn7x7ChZIwf/gZkf8E+0";

        var result = _passwordService.VerifyPassword(passwordWithSpace, hash);

        result.Should().BeFalse("password with leading space should not match");
    }

    [Fact]
    public void HashPassword_ShouldCreateValidArgon2Hash()
    {
        var password = "hunter2";

        var hash = _passwordService.HashPassword(password);

        hash.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=1$");
        hash.Split('$').Should().HaveCount(6);
    }

    [Fact]
    public void HashPassword_ThenVerify_ShouldSucceed()
    {
        var password = "hunter2";

        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue("freshly hashed password should verify");
    }

    [Fact]
    public void HashPassword_TwiceWithSamePassword_ShouldCreateDifferentHashes()
    {
        var password = "hunter2";

        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        hash1.Should().NotBe(hash2, "salts should be different");
    }

    [Theory]
    [InlineData("hunter2")]
    [InlineData("#KYA&Z!&v$QD$J5A&nEGAULiboryd$AbYPVmttQPbUjNp^kwx2X$vniEhB!8PvUx8Pcvshr*RbPYDs^rC6ptfiJ^^ZR8*&V*wV37Vgi6tGm4jDxQw^A788dwo&QGzm8")]
    [InlineData("тест")] // cyrillic
    [InlineData("測試")] // chinese
    [InlineData("🥺")] // emoji
    public void HashPassword_WithVariousInputs_ShouldHashAndVerify(string password)
    {
        var hash = _passwordService.HashPassword(password);
        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue($"password '{password}' should verify");
    }
    #endregion

    #region BCrypt Tests (Backwards Compatibility)
    [Fact]
    public void VerifyPassword_WithBCryptHash_ShouldSucceed()
    {
        var password = "hunter2";

        var bcryptHash = BCrypt.Net.BCrypt.HashPassword(password, 12);

        var result = _passwordService.VerifyPassword(password, bcryptHash);

        result.Should().BeTrue("BCrypt hashes should also work");
    }

    [Fact]
    public void VerifyPassword_WithBCryptHash_ShouldSucceed_PreComputed()
    {
        var password = "hunter2";
        var knownBcryptHash = "$2a$12$H8romZTbKyASvKsYBuOItucM7o2Zx0M70pLLW.QaYaci8UlWQa69e";

        var result = _passwordService.VerifyPassword(password, knownBcryptHash);

        result.Should().BeTrue("BCrypt hashes should also work");
    }
    #endregion

    #region Edge Cases
    [Fact]
    public void VerifyPassword_WithNullPassword_ShouldReturnFalse()
    {
        string? nullPassword = null;
        var hunter2Hash = "$argon2id$v=19$m=65536,t=3,p=1$CWGMkdKacy7FuDcGoJQS4g$mJFT0pW3Hw+tLcdAnW4aEmlTn7x7ChZIwf/gZkf8E+0";

        var result = _passwordService.VerifyPassword(nullPassword!, hunter2Hash);

        result.Should().BeFalse("null password should not verify");
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ShouldReturnFalse()
    {
        var password = "hunter2";
        string? nullHash = null;

        var result = _passwordService.VerifyPassword(password, nullHash!);

        result.Should().BeFalse("null hash should not verify");
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ShouldReturnFalse()
    {
        var password = "hunter2";
        var emptyHash = "";

        var result = _passwordService.VerifyPassword(password, emptyHash);

        result.Should().BeFalse("empty hash should not verify");
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ShouldReturnFalse()
    {
        var password = "hunter2";
        var malformedHash = "$argon2id$invalid";

        var result = _passwordService.VerifyPassword(password, malformedHash);

        result.Should().BeFalse("malformed hash should not verify");
    }

    [Fact]
    public void VerifyPassword_WithTruncatedHash_ShouldReturnFalse()
    {
        var password = "hunter2";
        var truncatedHash = "$argon2id$v=19$m=65536,t=3,p=1$CWGMkdKacy7FuDcGoJQS4g$mJFT0pW3Hw";

        var result = _passwordService.VerifyPassword(password, truncatedHash);

        result.Should().BeFalse("truncated hash should not verify");
    }
    #endregion
}
