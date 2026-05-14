using ExcelBotCs.Models.DTO.Members;

namespace ExcelBotCs.Tests.Models.DTO;

public class MemberDtoTests
{
    [Fact]
    public void IsAdmin_NoRoles_ReturnsFalse()
    {
        var sut = new MemberResponse();
        sut.IsAdmin.ShouldBe(false);
    }

    [Fact]
    public void IsAdmin_NoAdminRole_ReturnsFalse()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Member Role",
                    IsMember = true
                },
                new()
                {
                    Name = "Developer Role",
                    IsDeveloper = true
                }
            }
        };

        sut.IsAdmin.ShouldBe(false);
    }

    [Fact]
    public void IsAdmin_HasAdminRole_ReturnsTrue()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Member Role",
                    IsMember = true
                },
                new()
                {
                    Name = "Developer Role",
                    IsDeveloper = true
                },
                new()
                {
                    Name = "Admin Role",
                    IsAdmin = true
                }
            }
        };

        sut.IsAdmin.ShouldBe(true);
    }

    [Fact]
    public void IsMember_NoRoles_ReturnsFalse()
    {
        var sut = new MemberResponse();
        sut.IsMember.ShouldBe(false);
    }

    [Fact]
    public void IsMember_NoMemberRole_ReturnsFalse()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Developer Role",
                    IsDeveloper = true
                }
            }
        };

        sut.IsMember.ShouldBe(false);
    }

    [Fact]
    public void IsMember_HasAdminRole_ReturnsTrue()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Developer Role",
                    IsDeveloper = true
                },
                new()
                {
                    Name = "Admin Role",
                    IsAdmin = true
                }
            }
        };

        sut.IsMember.ShouldBe(true);
    }

    [Fact]
    public void IsMember_HasMemberRole_ReturnsTrue()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Developer Role",
                    IsDeveloper = true
                },
                new()
                {
                    Name = "Member Role",
                    IsMember = true
                }
            }
        };

        sut.IsMember.ShouldBe(true);
    }

    [Fact]
    public void IsDeveloper_NoRoles_ReturnsFalse()
    {
        var sut = new MemberResponse();
        sut.IsDeveloper.ShouldBe(false);
    }

    [Fact]
    public void IsDeveloper_NoDeveloperRole_ReturnsFalse()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Member Role",
                    IsMember = true
                },
                new()
                {
                    Name = "Admin Role",
                    IsAdmin = true
                }
            }
        };

        sut.IsDeveloper.ShouldBe(false);
    }

    [Fact]
    public void IsDeveloper_HasAdminRole_ReturnsTrue()
    {
        var sut = new MemberResponse
        {
            Roles = new List<MemberRoleDto>
            {
                new()
                {
                    Name = "test1"
                },
                new()
                {
                    Name = "Member Role",
                    IsMember = true
                },
                new()
                {
                    Name = "Developer Role",
                    IsDeveloper = true
                },
                new()
                {
                    Name = "Admin Role",
                    IsAdmin = true
                }
            }
        };

        sut.IsDeveloper.ShouldBe(true);
    }
}