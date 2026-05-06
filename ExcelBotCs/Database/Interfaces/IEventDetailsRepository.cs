using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Database.Interfaces;

public interface IEventDetailsRepository : IBaseRepository<EventDetails>
{
    Task<List<EventDetails>> GetFutureByParticipantAsync(ulong discordId);
}