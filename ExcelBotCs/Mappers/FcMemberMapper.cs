using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class FcMemberMapper
{
    public static FcMemberDto ToDto(FcMember fcMember)
    {
        return new FcMemberDto()
        {
            Id = fcMember.Id,
            Name = fcMember.Name,
            Bio = fcMember.Bio,
            Avatar = fcMember.Avatar,
            CharacterId = fcMember.CharacterId,
            FcRank = fcMember.FcRank,
            Title = fcMember.Title,
            LastSynchronisation = fcMember.LastSynchronisation,
        };
    }

    public static FcMember ToEntity(FcMemberDto fcMember)
    {
        return new FcMember()
        {
            Id = fcMember.Id,
            Name = fcMember.Name,
            Bio = fcMember.Bio,
            Avatar = fcMember.Avatar,
            CharacterId = fcMember.CharacterId,
            FcRank = fcMember.FcRank,
            Title = fcMember.Title,
            LastSynchronisation = fcMember.LastSynchronisation,
        };
    }
}