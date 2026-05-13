using System.Text;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Extensions;

public static class FcEventExtensions
{
    public static string CreateUpcomingRosterMessage(this Event fcEvent)
    {
        if (fcEvent.Groups == null || fcEvent.Groups.Count == 0)
            throw new ArgumentException("No groups selected for event. Unable to post a roster");

        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine($"**Upcoming roster for: {fcEvent.Name}**");
        messageBuilder.AppendLine($"**Date:** {fcEvent.StartDate.ToLongDiscordDateLongTime()}");
        messageBuilder.AppendLine($"**In:** {fcEvent.StartDate.ToRelativeDiscordTime()}");
        messageBuilder.AppendLine($"**Duration:** {fcEvent.Duration} minutes");
        messageBuilder.AppendLine();

        if (fcEvent.Groups.Count > 1)
        {
            foreach (var fcEventGroup in fcEvent.Groups)
            {
                var participants = fcEventGroup.Participants;

                messageBuilder.AppendLine($"{fcEventGroup.Name}:");
                AppendRoleMentions(messageBuilder, participants);
            }
        }
        else
        {
            var participants = fcEvent.Groups.First().Participants;
            AppendRoleMentions(messageBuilder, participants);
        }

        return messageBuilder.ToString();
    }

    private static void AppendRoleMentions(StringBuilder messageBuilder, List<EventParticipant> participants)
    {
        messageBuilder.AppendLine($"{Constants.TankRoleEmote} {RoleMentions(participants, Role.Tank)}");
        messageBuilder.AppendLine($"{Constants.HealerRoleEmote} {RoleMentions(participants, Role.Healer)}");
        messageBuilder.AppendLine($"{Constants.MeleeRoleEmote} {RoleMentions(participants, Role.Melee)}");
        messageBuilder.AppendLine($"{Constants.RangedRoleEmote} {RoleMentions(participants, Role.Ranged)}");
        messageBuilder.AppendLine($"{Constants.CasterRoleEmote} {RoleMentions(participants, Role.Caster)}");
    }

    private static string RoleMentions(List<EventParticipant> eventParticipants, Role role)
    {
        if (eventParticipants.IsNullOrEmpty())
            return string.Empty;

        var messageBuilder = new StringBuilder();

        var participants = eventParticipants.Where(p => p.Role == role).ToList();
        foreach (var participant in participants)
            messageBuilder.Append($"<@{participant.DiscordUserId}> ");

        return messageBuilder.ToString();
    }
}