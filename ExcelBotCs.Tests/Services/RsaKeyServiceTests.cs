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

[TestFixture]
public class RsaKeyServiceTests : MongoDbTest
{
    private RsaKeyService _rsaKeyService = null!;
    private IOptions<DatabaseOptions> _databaseOptions = null!;
    private JwtOptions _jwtOptions = null!;

    protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions;
    }

    [SetUp]
    public void Setup()
    {
        _rsaKeyService = new RsaKeyService(_databaseOptions);
        _jwtOptions = new JwtOptions
        {
            RsaPrivateKeyLocation = "/fake/path/private.pem",
            RsaPublicKeyLocation = "/fake/path/public.pem",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };
    }

    #region EnsureRsaKeysPresent Tests

    [Test]
    public void EnsureRsaKeysPresent_CreatesKeysWhenNoneExist()
    {
        // Act
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        // Assert - Should be able to retrieve keys
        using var publicRsa = _rsaKeyService.GetPublicRsa();
        using var privateRsa = _rsaKeyService.GetPrivateRsa();

        Assert.That(publicRsa, Is.Not.Null);
        Assert.That(privateRsa, Is.Not.Null);
    }

    [Test]
    public void EnsureRsaKeysPresent_DoesNotDuplicateExistingKeys()
    {
        // Arrange - Create initial keys
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        using (var initialPublicRsa = _rsaKeyService.GetPublicRsa())
        {
            var initialPublicKey = initialPublicRsa.ExportSubjectPublicKeyInfo();

            // Act - Call again
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

            // Assert - Should return same keys
            using var secondPublicRsa = _rsaKeyService.GetPublicRsa();
            var secondPublicKey = secondPublicRsa.ExportSubjectPublicKeyInfo();

            Assert.That(secondPublicKey, Is.EqualTo(initialPublicKey));
        }
    }

    [Test]
    public void EnsureRsaKeysPresent_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() =>
        {
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
            _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        });
    }

    #endregion

    #region GetPublicRsa Tests

    [Test]
    public void GetPublicRsa_ThrowsWhenNoKeysExist()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var rsa = _rsaKeyService.GetPublicRsa();
        });
    }

    [Test]
    public void GetPublicRsa_ReturnsValidRsaKey()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        // Act
        using var rsa = _rsaKeyService.GetPublicRsa();

        // Assert
        Assert.That(rsa, Is.Not.Null);
        Assert.That(rsa.KeySize, Is.EqualTo(2048));

        // Verify it's a valid public key by checking it can export public key info
        Assert.DoesNotThrow(() => rsa.ExportSubjectPublicKeyInfo());
    }

    [Test]
    public void GetPublicRsa_ReturnsConsistentKey()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        // Act
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

        // Assert
        Assert.That(secondKey, Is.EqualTo(firstKey));
    }

    #endregion

    #region GetPrivateRsa Tests

    [Test]
    public void GetPrivateRsa_ThrowsWhenNoKeysExist()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var rsa = _rsaKeyService.GetPrivateRsa();
        });
    }

    [Test]
    public void GetPrivateRsa_ReturnsValidRsaKey()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        // Act
        using var rsa = _rsaKeyService.GetPrivateRsa();

        // Assert
        Assert.That(rsa, Is.Not.Null);
        Assert.That(rsa.KeySize, Is.EqualTo(2048));

        // Verify it's a valid private key by checking it can export private key
        Assert.DoesNotThrow(() => rsa.ExportPkcs8PrivateKey());
    }

    [Test]
    public void GetPrivateRsa_ReturnsConsistentKey()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        // Act
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

        // Assert
        Assert.That(secondKey, Is.EqualTo(firstKey));
    }

    [Test]
    public void GetPrivateAndPublicRsa_AreKeyPair()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        // Act
        using var privateRsa = _rsaKeyService.GetPrivateRsa();
        using var publicRsa = _rsaKeyService.GetPublicRsa();

        // Test encryption/decryption to verify they're a pair
        var testData = "Test data for RSA encryption"u8.ToArray();
        var encrypted = publicRsa.Encrypt(testData, RSAEncryptionPadding.OaepSHA256);
        var decrypted = privateRsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);

        // Assert
        Assert.That(decrypted, Is.EqualTo(testData));
    }

    #endregion

    #region GenerateJwt Tests

    [Test]
    public void GenerateJwt_CreatesValidToken()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456"),
            new(ClaimTypes.Name, "TestUser")
        };

        // Act
        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        // Assert
        Assert.That(token, Is.Not.Null);
        Assert.That(token, Is.Not.Empty);
        Assert.That(token.Split('.').Length, Is.EqualTo(3)); // JWT has 3 parts
    }

    [Test]
    public void GenerateJwt_TokenCanBeValidated()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456"),
            new(ClaimTypes.Name, "TestUser")
        };

        // Act
        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        // Assert - Validate the token
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
        Assert.That(principal, Is.Not.Null);
        Assert.That(validatedToken, Is.Not.Null);
    }

    [Test]
    public void GenerateJwt_TokenContainsExpectedClaims()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456"),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Email, "test@example.com")
        };

        // Act
        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        // Assert - Decode and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // JWT serializes claims - check if any claim has the expected value
        Assert.That(jwtToken.Claims.Any(c => c.Value == "123456"), Is.True, "NameIdentifier claim not found");
        Assert.That(jwtToken.Claims.Any(c => c.Value == "TestUser"), Is.True, "Name claim not found");
        Assert.That(jwtToken.Claims.Any(c => c.Value == "test@example.com"), Is.True, "Email claim not found");
    }

    [Test]
    public void GenerateJwt_TokenHasCorrectIssuerAndAudience()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456")
        };

        // Act
        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.That(jwtToken.Issuer, Is.EqualTo(_jwtOptions.Issuer));
        Assert.That(jwtToken.Audiences.First(), Is.EqualTo(_jwtOptions.Audience));
    }

    [Test]
    public void GenerateJwt_TokenHasExpiration()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123456")
        };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.That(jwtToken.ValidTo, Is.Not.EqualTo(default(DateTime)));
        Assert.That(jwtToken.ValidTo, Is.GreaterThan(beforeGeneration.AddDays(6))); // Should be ~7 days
        Assert.That(jwtToken.ValidTo, Is.LessThan(afterGeneration.AddDays(8))); // Allow some buffer
    }

    [Test]
    public void GenerateJwt_WithEmptyClaims()
    {
        // Arrange
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");
        var claims = new List<Claim>();

        // Act
        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        // Assert
        Assert.That(token, Is.Not.Null);
        Assert.That(token, Is.Not.Empty);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.That(jwtToken, Is.Not.Null);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void FullWorkflow_CreateKeysGenerateAndValidateToken()
    {
        // Arrange & Act - Full workflow
        _rsaKeyService.EnsureRsaKeysPresent(_jwtOptions, "/fake/path");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Name, "IntegrationTestUser"),
            new(ClaimTypes.Role, "Admin")
        };

        var token = _rsaKeyService.GenerateJwt(_jwtOptions, claims);

        // Assert - Validate token with public key
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

        Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("999"));
        Assert.That(principal.FindFirst(ClaimTypes.Name)?.Value, Is.EqualTo("IntegrationTestUser"));
        Assert.That(principal.FindFirst(ClaimTypes.Role)?.Value, Is.EqualTo("Admin"));
    }

    #endregion
}