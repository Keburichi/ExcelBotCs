using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Tests.Models.DTO;

[TestFixture]
public class MemberDtoTests
{
    [Test]
    public void IsAdmin_NoRoles_ReturnsFalse()
    {
        var sut = new MemberDto();
        Assert.That(sut.IsAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_NoAdminRole_ReturnsFalse()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_HasAdminRole_ReturnsTrue()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsAdmin, Is.True);
    }

    [Test]
    public void IsMember_NoRoles_ReturnsFalse()
    {
        var sut = new MemberDto();
        Assert.That(sut.IsMember, Is.False);
    }

    [Test]
    public void IsMember_NoMemberRole_ReturnsFalse()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsMember, Is.False);
    }

    [Test]
    public void IsMember_HasAdminRole_ReturnsTrue()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsMember, Is.True);
    }

    [Test]
    public void IsMember_HasMemberRole_ReturnsTrue()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsMember, Is.True);
    }

    [Test]
    public void IsDeveloper_NoRoles_ReturnsFalse()
    {
        var sut = new MemberDto();
        Assert.That(sut.IsDeveloper, Is.False);
    }

    [Test]
    public void IsDeveloper_NoDeveloperRole_ReturnsFalse()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsDeveloper, Is.False);
    }

    [Test]
    public void IsDeveloper_HasAdminRole_ReturnsTrue()
    {
        var sut = new MemberDto
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

        Assert.That(sut.IsDeveloper, Is.True);
    }
}