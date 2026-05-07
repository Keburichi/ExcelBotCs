using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Services;

[Collection("MongoDB")]
public class RsaKeyServiceTests : MongoDbTest
{
    private RsaKeyService _rsaKeyService = null!;
    private IOptions<DatabaseOptions> _databaseOptions = null!;
    private JwtOptions _jwtOptions = null!;

    public RsaKeyServiceTests(MongoDbFixture fixture) : base(fixture)
    {
    }

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions;
    }

    protected override Task OnAfterInitializeAsync()
    {
        _rsaKeyService = new RsaKeyService(_databaseOptions);
        _jwtOptions = new JwtOptions
        {
            RsaPrivateKeyLocation = "/fake/path/private.pem",
            RsaPublicKeyLocation = "/fake/path/public.pem",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };
        return Task.CompletedTask;
    }

    #region EnsureRsaKeysPresent Tests

    [Fact]
    public void EnsureRsaKeysPresent_CreatesKeysWhenNoneExist()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        using var publicRsa = _rsaKeyService.GetPublicRsa();
        using var privateRsa = _rsaKeyService.GetPrivateRsa();

        publicRsa.ShouldNotBeNull();
        privateRsa.ShouldNotBeNull();
    }

    [Fact]
    public void EnsureRsaKeysPresent_DoesNotDuplicateExistingKeys()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        using (var initialPublicRsa = _rsaKeyService.GetPublicRsa())
        {
            var initialPublicKey = initialPublicRsa.ExportSubjectPublicKeyInfo();

            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

            using var secondPublicRsa = _rsaKeyService.GetPublicRsa();
            var secondPublicKey = secondPublicRsa.ExportSubjectPublicKeyInfo();

            secondPublicKey.ShouldBe(initialPublicKey);
        }
    }

    [Fact]
    public void EnsureRsaKeysPresent_CanBeCalledMultipleTimes()
    {
        Should.NotThrow(() =>
        {
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        });
    }

    #endregion

    #region GetPublicRsa Tests

    [Fact]
    public void GetPublicRsa_ThrowsWhenNoKeysExist()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            using var rsa = _rsaKeyService.GetPublicRsa();
        });
    }

    [Fact]
    public void GetPublicRsa_ReturnsValidRsaKey()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        using var rsa = _rsaKeyService.GetPublicRsa();

        rsa.ShouldNotBeNull();
        rsa.KeySize.ShouldBe(2048);

        Should.NotThrow(() => rsa.ExportSubjectPublicKeyInfo());
    }

    [Fact]
    public void GetPublicRsa_ReturnsConsistentKey()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        byte[] firstKey;
        byte[] secondKey;

        using (var rsa1 = _rsaKeyService.GetPublicRsa())
        {
            firstKey = rsa1.ExportSubjectPublicKeyInfo();
        }

        using (var rsa2 = _rsaKeyService.GetPublicRsa())
        {
            secondKey = rsa2.ExportSubjectPublicKeyInfo();
        }

        secondKey.ShouldBe(firstKey);
    }

    #endregion

    #region GetPrivateRsa Tests

    [Fact]
    public void GetPrivateRsa_ThrowsWhenNoKeysExist()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            using var rsa = _rsaKeyService.GetPrivateRsa();
        });
    }

    [Fact]
    public void GetPrivateRsa_ReturnsValidRsaKey()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        using var rsa = _rsaKeyService.GetPrivateRsa();

        rsa.ShouldNotBeNull();
        rsa.KeySize.ShouldBe(2048);

        Should.NotThrow(() => rsa.ExportPkcs8PrivateKey());
    }

    [Fact]
    public void GetPrivateRsa_ReturnsConsistentKey()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        byte[] firstKey;
        byte[] secondKey;

        using (var rsa1 = _rsaKeyService.GetPrivateRsa())
        {
            firstKey = rsa1.ExportPkcs8PrivateKey();
        }

        using (var rsa2 = _rsaKeyService.GetPrivateRsa())
        {
            secondKey = rsa2.ExportPkcs8PrivateKey();
        }

        secondKey.ShouldBe(firstKey);
    }

    [Fact]
    public void GetPrivateAndPublicRsa_AreKeyPair()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        using var privateRsa = _rsaKeyService.GetPrivateRsa();
        using var publicRsa = _rsaKeyService.GetPublicRsa();

        var testData = "Test data for RSA encryption"u8.ToArray();
        var encrypted = publicRsa.Encrypt(testData, RSAEncryptionPadding.OaepSHA256);
        var decrypted = privateRsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);

        decrypted.ShouldBe(testData);
    }

    #endregion

    #region GenerateJwt Tests

    [Fact]
    public void GenerateJwt_CreatesValidToken()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456"),
            new(ClaimTypes.Name, "TestUser")
        };

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        token.ShouldNotBeNull();
        token.ShouldNotBeEmpty();
        token.Split('.').Length.ShouldBe(3);
    }

    [Fact]
    public void GenerateJwt_TokenCanBeValidated()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456"),
            new(ClaimTypes.Name, "TestUser")
        };

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        var tokenHandler = new JwtSecurityTokenHandler();
        using var publicRsa = _rsaKeyService.GetPublicRsa();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        principal.ShouldNotBeNull();
        validatedToken.ShouldNotBeNull();
    }

    [Fact]
    public void GenerateJwt_TokenContainsExpectedClaims()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456"),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Email, "test@example.com")
        };

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Any(c => c.Value == "123456").ShouldBeTrue();
        jwtToken.Claims.Any(c => c.Value == "TestUser").ShouldBeTrue();
        jwtToken.Claims.Any(c => c.Value == "test@example.com").ShouldBeTrue();
    }

    [Fact]
    public void GenerateJwt_TokenHasCorrectIssuerAndAudience()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456")
        };

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.ShouldBe(_jwtOptions.Issuer);
        jwtToken.Audiences.First().ShouldBe(_jwtOptions.Audience);
    }

    [Fact]
    public void GenerateJwt_TokenHasExpiration()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456")
        };
        var beforeGeneration = DateTime.UtcNow;

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);
        var afterGeneration = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.ShouldNotBe(default(DateTime));
        jwtToken.ValidTo.ShouldBeGreaterThan(beforeGeneration.AddDays(6));
        jwtToken.ValidTo.ShouldBeLessThan(afterGeneration.AddDays(8));
    }

    [Fact]
    public void GenerateJwt_WithEmptyClaims()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>();

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        token.ShouldNotBeNull();
        token.ShouldNotBeEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.ShouldNotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_CreateKeysGenerateAndValidateToken()
    {
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Name, "IntegrationTestUser"),
            new(ClaimTypes.Role, "Admin")
        };

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        var tokenHandler = new JwtSecurityTokenHandler();
        using var publicRsa = _rsaKeyService.GetPublicRsa();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("999");
        principal.FindFirst(ClaimTypes.Name)?.Value.ShouldBe("IntegrationTestUser");
        principal.FindFirst(ClaimTypes.Role)?.Value.ShouldBe("Admin");
    }

    #endregion
}
