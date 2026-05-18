using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class FcMemberMappingExtensions
{
    public static FcMemberDto ToDto(this FcMember fcMember)
    {
        return new FcMemberDto
        {
            Id = fcMember.Id,
            Name = fcMember.Name,
            Bio = fcMember.Bio,
            Avatar = fcMember.Avatar,
            CharacterId = fcMember.CharacterId,
            FcRank = fcMember.FcRank,
            Title = fcMember.Title,
            LastSynchronisation = fcMember.LastSynchronisation
        };
    }

    public static FcMember ToEntity(this FcMemberDto fcMember)
    {
        return new FcMember
        {
            Id = fcMember.Id,
            Name = fcMember.Name,
            Bio = fcMember.Bio,
            Avatar = fcMember.Avatar,
            CharacterId = fcMember.CharacterId,
            FcRank = fcMember.FcRank,
            Title = fcMember.Title,
            LastSynchronisation = fcMember.LastSynchronisation
        };
    }
}