using System.Security.Claims;
using ExcelBotCs.Extensions;
using ExcelBotCs.TestFramework.Attributes;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class ClaimExtensionsTests
{
    [Test]
    public void GetDiscordId_WhenClaimExists_ShouldReturnValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123456789")
        };

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo("123456789"));
    }

    [Test]
    public void GetDiscordId_WhenClaimDoesNotExist_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordNameClaimType, "TestUser")
        };

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDiscordId_WhenListIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestIsNullOrEmptyString]
    public void GetDiscordId_WithNullOrEmptyClaimValue_ShouldReturnValue(string claimValue)
    {
        // Arrange - Claim constructor doesn't accept null, so skip creating claim for null case
        var claims = new List<Claim>();

        if (claimValue != null) claims.Add(new Claim(ClaimExtensions.DiscordIdClaimType, claimValue));

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo(claimValue ?? string.Empty));
    }

    [Test]
    public void GetDiscordId_WithMultipleClaims_ShouldReturnCorrectValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordNameClaimType, "TestUser"),
            new(ClaimExtensions.DiscordIdClaimType, "123456789"),
            new(ClaimExtensions.DiscordAvatarClaimType, "avatar_url"),
            new("SomeOtherClaim", "SomeValue")
        };

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo("123456789"));
    }

    [Test]
    public void GetDiscordName_WhenClaimExists_ShouldReturnValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordNameClaimType, "JohnDoe")
        };

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo("JohnDoe"));
    }

    [Test]
    public void GetDiscordName_WhenClaimDoesNotExist_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123456789")
        };

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDiscordName_WhenListIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestIsNullOrEmptyString]
    public void GetDiscordName_WithNullOrEmptyClaimValue_ShouldReturnValue(string claimValue)
    {
        // Arrange - Claim constructor doesn't accept null, so skip creating claim for null case
        var claims = new List<Claim>();

        if (claimValue != null) claims.Add(new Claim(ClaimExtensions.DiscordNameClaimType, claimValue));

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo(claimValue ?? string.Empty));
    }

    [Test]
    public void GetDiscordName_WithMultipleClaims_ShouldReturnCorrectValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123456789"),
            new(ClaimExtensions.DiscordNameClaimType, "JohnDoe"),
            new(ClaimExtensions.DiscordAvatarClaimType, "avatar_url")
        };

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo("JohnDoe"));
    }

    [Test]
    public void GetDiscordAvatar_WhenClaimExists_ShouldReturnValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordAvatarClaimType, "https://cdn.discordapp.com/avatars/123/abc.png")
        };

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo("https://cdn.discordapp.com/avatars/123/abc.png"));
    }

    [Test]
    public void GetDiscordAvatar_WhenClaimDoesNotExist_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123456789")
        };

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDiscordAvatar_WhenListIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestIsNullOrEmptyString]
    public void GetDiscordAvatar_WithNullOrEmptyClaimValue_ShouldReturnValue(string claimValue)
    {
        // Arrange - Claim constructor doesn't accept null, so skip creating claim for null case
        var claims = new List<Claim>();

        if (claimValue != null) claims.Add(new Claim(ClaimExtensions.DiscordAvatarClaimType, claimValue));

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo(claimValue ?? string.Empty));
    }

    [Test]
    public void GetDiscordAvatar_WithMultipleClaims_ShouldReturnCorrectValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123456789"),
            new(ClaimExtensions.DiscordNameClaimType, "JohnDoe"),
            new(ClaimExtensions.DiscordAvatarClaimType, "https://cdn.discordapp.com/avatars/123/abc.png")
        };

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo("https://cdn.discordapp.com/avatars/123/abc.png"));
    }

    [Test]
    public void ClaimTypeConstants_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.That(ClaimExtensions.DiscordIdClaimType,
            Is.EqualTo("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));
        Assert.That(ClaimExtensions.DiscordNameClaimType,
            Is.EqualTo("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"));
        Assert.That(ClaimExtensions.DiscordAvatarClaimType, Is.EqualTo("urn:discord:avatar:url"));
    }

    [Test]
    public void GetDiscordId_WithDuplicateClaims_ShouldReturnFirstMatch()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "111111111"),
            new(ClaimExtensions.DiscordIdClaimType, "222222222")
        };

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo("111111111"));
    }

    [Test]
    public void GetDiscordName_WithDuplicateClaims_ShouldReturnFirstMatch()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordNameClaimType, "FirstName"),
            new(ClaimExtensions.DiscordNameClaimType, "SecondName")
        };

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo("FirstName"));
    }

    [Test]
    public void GetDiscordAvatar_WithDuplicateClaims_ShouldReturnFirstMatch()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordAvatarClaimType, "avatar1.png"),
            new(ClaimExtensions.DiscordAvatarClaimType, "avatar2.png")
        };

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo("avatar1.png"));
    }

    [Test]
    public void GetAllMethods_WithCompleteClaimSet_ShouldReturnAllValues()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123456789"),
            new(ClaimExtensions.DiscordNameClaimType, "TestUser"),
            new(ClaimExtensions.DiscordAvatarClaimType, "https://cdn.discordapp.com/avatars/123/abc.png")
        };

        // Act
        var discordId = claims.GetDiscordId();
        var discordName = claims.GetDiscordName();
        var discordAvatar = claims.GetDiscordAvatar();

        // Assert
        Assert.That(discordId, Is.EqualTo("123456789"));
        Assert.That(discordName, Is.EqualTo("TestUser"));
        Assert.That(discordAvatar, Is.EqualTo("https://cdn.discordapp.com/avatars/123/abc.png"));
    }

    [Test]
    public void GetAllMethods_WithNoMatchingClaims_ShouldReturnEmptyStrings()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("SomeClaim", "SomeValue"),
            new("AnotherClaim", "AnotherValue")
        };

        // Act
        var discordId = claims.GetDiscordId();
        var discordName = claims.GetDiscordName();
        var discordAvatar = claims.GetDiscordAvatar();

        // Assert
        Assert.That(discordId, Is.EqualTo(string.Empty));
        Assert.That(discordName, Is.EqualTo(string.Empty));
        Assert.That(discordAvatar, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDiscordId_WithSpecialCharactersInValue_ShouldReturnValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordIdClaimType, "123-456-789_abc@xyz")
        };

        // Act
        var result = claims.GetDiscordId();

        // Assert
        Assert.That(result, Is.EqualTo("123-456-789_abc@xyz"));
    }

    [Test]
    public void GetDiscordName_WithSpecialCharactersInValue_ShouldReturnValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordNameClaimType, "User#1234")
        };

        // Act
        var result = claims.GetDiscordName();

        // Assert
        Assert.That(result, Is.EqualTo("User#1234"));
    }

    [Test]
    public void GetDiscordAvatar_WithGifAvatar_ShouldReturnValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimExtensions.DiscordAvatarClaimType, "https://cdn.discordapp.com/avatars/123/a_abc.gif")
        };

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        Assert.That(result, Is.EqualTo("https://cdn.discordapp.com/avatars/123/a_abc.gif"));
    }
}