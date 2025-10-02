using System.Text;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Extensions;

public static class FcEventExtensions
{
    public static string CreateUpcomingRosterMessage(this Event fcEvent)
    {
        var messageBuilder = new StringBuilder();
        
        messageBuilder.AppendLine($"**Upcoming roster for: {fcEvent.Name}**");
        messageBuilder.AppendLine($"**Date:** {fcEvent.StartDate.ToLongDiscordDateLongTime()}");
        messageBuilder.AppendLine($"**In:** {fcEvent.StartDate.ToRelativeDiscordTime()}");
        messageBuilder.AppendLine($"**Duration:** {fcEvent.Duration} minutes");
        messageBuilder.AppendLine();
        messageBuilder.AppendLine($":RoleTank: {RoleMentions(fcEvent.Participants, Role.Tank)}");
        messageBuilder.AppendLine($":RoleHealer: {RoleMentions(fcEvent.Participants, Role.Healer)}");
        messageBuilder.AppendLine($":RoleMelee: {RoleMentions(fcEvent.Participants, Role.Melee)}");
        messageBuilder.AppendLine($":RoleCaster: {RoleMentions(fcEvent.Participants, Role.Caster)}");
        messageBuilder.AppendLine($":RoleRanged: {RoleMentions(fcEvent.Participants, Role.Ranged)}");
        
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