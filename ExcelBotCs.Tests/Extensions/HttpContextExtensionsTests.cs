using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Http;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class HttpContextExtensionsTests
{
    [SetUp]
    public void Setup()
    {
        _httpContext = new DefaultHttpContext();
    }

    private DefaultHttpContext _httpContext = null!;

    [Test]
    public void GetCurrentMember_WhenMemberExists_ShouldReturnMember()
    {
        // Arrange
        var member = new Member
        {
            Id = "123",
            DiscordId = "456",
            DiscordName = "TestUser"
        };
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = member;

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(member.Id));
        Assert.That(result.DiscordId, Is.EqualTo(member.DiscordId));
        Assert.That(result.DiscordName, Is.EqualTo(member.DiscordName));
    }

    [Test]
    public void GetCurrentMember_WhenMemberDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetCurrentMember_WhenItemIsNotMember_ShouldReturnNull()
    {
        // Arrange
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = "Not a member object";

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetCurrentMember_WhenItemIsNull_ShouldReturnNull()
    {
        // Arrange
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = null;

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetCurrentMember_WhenMultipleItemsExist_ShouldReturnCorrectMember()
    {
        // Arrange
        var member = new Member
        {
            Id = "123",
            DiscordId = "456",
            DiscordName = "TestUser"
        };
        _httpContext.Items["OtherKey"] = "OtherValue";
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = member;
        _httpContext.Items["AnotherKey"] = 42;

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(member.Id));
    }

    [Test]
    public void GetCurrentMember_WithComplexMemberObject_ShouldReturnMemberWithAllProperties()
    {
        // Arrange
        var adminRole = new MemberRole { Id = "role1", DiscordId = "admin", Name = "Admin", IsAdmin = true };
        var memberRole = new MemberRole { Id = "role2", DiscordId = "member", Name = "Member", IsMember = true };
        var fight = new Fight { Id = "fight1", Name = "Test Fight" };

        var member = new Member
        {
            Id = "123",
            DiscordId = "456",
            DiscordName = "TestUser",
            LodestoneId = "789",
            Roles = new List<MemberRole> { adminRole, memberRole },
            Experience = new List<Fight> { fight }
        };
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = member;

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(member.Id));
        Assert.That(result.DiscordId, Is.EqualTo(member.DiscordId));
        Assert.That(result.DiscordName, Is.EqualTo(member.DiscordName));
        Assert.That(result.LodestoneId, Is.EqualTo(member.LodestoneId));
        Assert.That(result.Roles, Has.Count.EqualTo(member.Roles.Count));
        Assert.That(result.Experience, Has.Count.EqualTo(member.Experience.Count));
    }
}