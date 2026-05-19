using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Bosses;
using ExcelBotCs.Models.DTO.Fights;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Mappers.Bosses;

public static class BossMappingExtensions
{
    public static BossResponse ToBossResponse(this Boss boss, List<Fight>? fights = null)
    {
        return new BossResponse
        {
            Id = boss.Id,
            Name = boss.Name,
            Description = boss.Description,
            ImageUrl = boss.ImageUrl,
            IsUltimate = boss.IsUltimate,
            FFLogsExpansionId = boss.FFLogsExpansionId,
            Fights = fights?.Select(f => f.ToFightSummaryResponse()).ToList() ?? new()
        };
    }

    public static Boss ToEntity(this CreateBossRequest request)
    {
        return new Boss
        {
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsUltimate = request.IsUltimate,
            NormalizationKey = FightNormalization.GetNormalizationKey(request.Name)
        };
    }

    public static void ApplyUpdate(this Boss boss, UpdateBossRequest request)
    {
        if (request.Name != null)
        {
            boss.Name = request.Name;
            boss.NormalizationKey = FightNormalization.GetNormalizationKey(request.Name);
        }

        if (request.Description != null)
            boss.Description = request.Description;

        if (request.ImageUrl != null)
            boss.ImageUrl = request.ImageUrl;

        if (request.IsUltimate.HasValue)
            boss.IsUltimate = request.IsUltimate.Value;
    }

    private static FightSummaryResponse ToFightSummaryResponse(this Fight fight)
    {
        return new FightSummaryResponse
        {
            Id = fight.Id,
            Name = fight.Name,
            Type = fight.Type,
            FFLogsEncounterId = fight.FFLogsEncounterId,
            IsFrozen = fight.IsFrozen
        };
    }
}
