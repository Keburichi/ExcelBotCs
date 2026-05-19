using ExcelBotCs.Models.DTO.Bosses;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IBossesController
{
    Task<ActionResult<List<BossResponse>>> GetBosses();
    Task<ActionResult<BossResponse>> GetBoss(string id);
    Task<ActionResult<BossResponse>> CreateBoss(CreateBossRequest request);
    Task<ActionResult> UpdateBoss(string id, UpdateBossRequest request);
    Task<ActionResult> DeleteBoss(string id);
}
