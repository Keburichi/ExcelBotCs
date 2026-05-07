using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Http;

namespace ExcelBotCs.Tests.Extensions;

public class HttpContextExtensionsTests
{
    private DefaultHttpContext _httpContext = null!;

    public HttpContextExtensionsTests()
    {
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(member.Id);
        result.DiscordId.ShouldBe(member.DiscordId);
        result.DiscordName.ShouldBe(member.DiscordName);
    }

    [Fact]
    public void GetCurrentMember_WhenMemberDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentMember_WhenItemIsNotMember_ShouldReturnNull()
    {
        // Arrange
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = "Not a member object";

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentMember_WhenItemIsNull_ShouldReturnNull()
    {
        // Arrange
        _httpContext.Items[CurrentMemberAccessor.HttpContextItemKey] = null;

        // Act
        var result = _httpContext.GetCurrentMember();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(member.Id);
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(member.Id);
        result.DiscordId.ShouldBe(member.DiscordId);
        result.DiscordName.ShouldBe(member.DiscordName);
        result.LodestoneId.ShouldBe(member.LodestoneId);
        result.Roles.Count.ShouldBe(member.Roles.Count);
        result.Experience.Count.ShouldBe(member.Experience.Count);
    }
}
