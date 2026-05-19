using ExcelBotCs.Models.DTO.Fights;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IFightsController
{
    Task<ActionResult<List<FightResponse>>> GetFights();
    Task<ActionResult<FightResponse>> GetFight(string id);
    Task<ActionResult<FightResponse>> CreateFight(CreateFightRequest request);
    Task<ActionResult> UpdateFight(string id, UpdateFightRequest request);
    Task<ActionResult> DeleteFight(string id);
}
