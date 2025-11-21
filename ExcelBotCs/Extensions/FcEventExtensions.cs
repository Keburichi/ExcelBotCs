using System.Text;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Extensions;

public static class FcEventExtensions
{
    public static string CreateUpcomingRosterMessage(this Event fcEvent)
    {
        var messageBuilder = new StringBuilder();

        // Get next upcoming occurrence or first occurrence
        var occurrence = fcEvent.Occurrences
                             ?.Where(o => o.Status == OccurrenceStatus.Scheduled && o.OccurrenceDate >= DateTime.UtcNow)
                             .OrderBy(o => o.OccurrenceDate)
                             .FirstOrDefault()
                         ?? fcEvent.Occurrences?.FirstOrDefault();

        if (occurrence == null) return $"**No upcoming occurrences for: {fcEvent.Name}**";

        var participants = occurrence.Participants ?? new List<EventParticipant>();

        messageBuilder.AppendLine($"**Upcoming roster for: {fcEvent.Name}**");
        messageBuilder.AppendLine($"**Date:** {occurrence.OccurrenceDate.ToLongDiscordDateLongTime()}");
        messageBuilder.AppendLine($"**In:** {occurrence.OccurrenceDate.ToRelativeDiscordTime()}");
        messageBuilder.AppendLine($"**Duration:** {fcEvent.Duration} minutes");
        messageBuilder.AppendLine();
        messageBuilder.AppendLine($":RoleTank: {RoleMentions(participants, Role.Tank)}");
        messageBuilder.AppendLine($":RoleHealer: {RoleMentions(participants, Role.Healer)}");
        messageBuilder.AppendLine($":RoleMelee: {RoleMentions(participants, Role.Melee)}");
        messageBuilder.AppendLine($":RoleCaster: {RoleMentions(participants, Role.Caster)}");
        messageBuilder.AppendLine($":RoleRanged: {RoleMentions(participants, Role.Ranged)}");

        return messageBuilder.ToString();
    }

    private static string RoleMentions(List<EventParticipant> eventParticipants, Role role)
    {
        var messageBuilder = new StringBuilder();
        
        var participants = eventParticipants.Where(p => p.Role == role).ToList();
        foreach (var participant in participants)
            messageBuilder.Append($"<@{participant.DiscordUserId}> ");
        
        return messageBuilder.ToString();
    }
}