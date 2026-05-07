using System.Security.Claims;
using ExcelBotCs.Extensions;
using ExcelBotCs.TestFramework.TestData;

namespace ExcelBotCs.Tests.Extensions;

public class ClaimExtensionsTests
{
    [Fact]
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
        result.ShouldBe("123456789");
    }

    [Fact]
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
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetDiscordId_WhenListIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var result = claims.GetDiscordId();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void GetDiscordId_WithNullOrEmptyClaimValue_ShouldReturnValue(string? claimValue)
    {
        // Arrange - Claim constructor doesn't accept null, so skip creating claim for null case
        var claims = new List<Claim>();

        if (claimValue != null) claims.Add(new Claim(ClaimExtensions.DiscordIdClaimType, claimValue));

        // Act
        var result = claims.GetDiscordId();

        // Assert
        result.ShouldBe(claimValue ?? string.Empty);
    }

    [Fact]
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
        result.ShouldBe("123456789");
    }

    [Fact]
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
        result.ShouldBe("JohnDoe");
    }

    [Fact]
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
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetDiscordName_WhenListIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var result = claims.GetDiscordName();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void GetDiscordName_WithNullOrEmptyClaimValue_ShouldReturnValue(string? claimValue)
    {
        // Arrange - Claim constructor doesn't accept null, so skip creating claim for null case
        var claims = new List<Claim>();

        if (claimValue != null) claims.Add(new Claim(ClaimExtensions.DiscordNameClaimType, claimValue));

        // Act
        var result = claims.GetDiscordName();

        // Assert
        result.ShouldBe(claimValue ?? string.Empty);
    }

    [Fact]
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
        result.ShouldBe("JohnDoe");
    }

    [Fact]
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
        result.ShouldBe("https://cdn.discordapp.com/avatars/123/abc.png");
    }

    [Fact]
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
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetDiscordAvatar_WhenListIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void GetDiscordAvatar_WithNullOrEmptyClaimValue_ShouldReturnValue(string? claimValue)
    {
        // Arrange - Claim constructor doesn't accept null, so skip creating claim for null case
        var claims = new List<Claim>();

        if (claimValue != null) claims.Add(new Claim(ClaimExtensions.DiscordAvatarClaimType, claimValue));

        // Act
        var result = claims.GetDiscordAvatar();

        // Assert
        result.ShouldBe(claimValue ?? string.Empty);
    }

    [Fact]
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
        result.ShouldBe("https://cdn.discordapp.com/avatars/123/abc.png");
    }

    [Fact]
    public void ClaimTypeConstants_ShouldHaveCorrectValues()
    {
        // Assert
        ClaimExtensions.DiscordIdClaimType
            .ShouldBe("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        ClaimExtensions.DiscordNameClaimType
            .ShouldBe("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        ClaimExtensions.DiscordAvatarClaimType.ShouldBe("urn:discord:avatar:url");
    }

    [Fact]
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
        result.ShouldBe("111111111");
    }

    [Fact]
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
        result.ShouldBe("FirstName");
    }

    [Fact]
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
        result.ShouldBe("avatar1.png");
    }

    [Fact]
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
        discordId.ShouldBe("123456789");
        discordName.ShouldBe("TestUser");
        discordAvatar.ShouldBe("https://cdn.discordapp.com/avatars/123/abc.png");
    }

    [Fact]
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
        discordId.ShouldBe(string.Empty);
        discordName.ShouldBe(string.Empty);
        discordAvatar.ShouldBe(string.Empty);
    }

    [Fact]
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
        result.ShouldBe("123-456-789_abc@xyz");
    }

    [Fact]
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
        result.ShouldBe("User#1234");
    }

    [Fact]
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
        result.ShouldBe("https://cdn.discordapp.com/avatars/123/a_abc.gif");
    }
}
