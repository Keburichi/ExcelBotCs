using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Tests.Models.Database;

[TestFixture]
public class MemberTests
{
    [Test]
    public void IsAdmin_NoRoles_ReturnsFalse()
    {
        var sut = new Member();
        Assert.That(sut.IsAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_NoAdminRole_ReturnsFalse()
    {
        var sut = new Member
        {
            Roles = new List<MemberRole>
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
        var sut = new Member
        {
            Roles = new List<MemberRole>
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
        var sut = new Member();
        Assert.That(sut.IsMember, Is.False);
    }

    [Test]
    public void IsMember_NoMemberRole_ReturnsFalse()
    {
        var sut = new Member
        {
            Roles = new List<MemberRole>
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
        var sut = new Member
        {
            Roles = new List<MemberRole>
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
        var sut = new Member
        {
            Roles = new List<MemberRole>
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
        var sut = new Member();
        Assert.That(sut.IsDeveloper, Is.False);
    }

    [Test]
    public void IsDeveloper_NoDeveloperRole_ReturnsFalse()
    {
        var sut = new Member
        {
            Roles = new List<MemberRole>
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
        var sut = new Member
        {
            Roles = new List<MemberRole>
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