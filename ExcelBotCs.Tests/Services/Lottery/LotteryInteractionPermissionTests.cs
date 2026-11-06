using System.Reflection;
using System.Runtime.CompilerServices;
using Discord.Interactions;
using ExcelBotCs.Extensions;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The slash-command permission sweep (spec §10, §11; criterion 24): officers keep <c>/lottery run</c>,
///     <c>/lottery remind</c> and <c>/lottery award</c>, and the member commands stay ungated because the service gates
///     them on FC membership instead. The Discord interaction context cannot be constructed without a live socket, so
///     the sweep asserts the two things that are observable from the wiring: the declared command surface, and that each
///     officer-only command really calls the officer gate. A command that loses its gate fails here.
/// </summary>
public class LotteryInteractionPermissionTests
{
    private static readonly string[] OfficerOnlyCommands = ["run", "remind", "award"];

    private static readonly string[] MemberCommands =
        ["guess", "unused", "luckydip", "change", "whoguessed", "view", "reminders"];

    [Fact]
    public void TheDeclaredCommandSurfaceIsExactlyTheAcceptedOne()
    {
        var declared = typeof(LotteryInteraction)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.GetCustomAttribute<SlashCommandAttribute>()?.Name)
            .Where(name => name is not null)
            .ToList();

        declared.ShouldBe(OfficerOnlyCommands.Concat(MemberCommands), ignoreOrder: true);
    }

    [Fact]
    public void EveryOfficerOnlyCommandChecksTheOfficerGate()
    {
        foreach (var command in OfficerOnlyCommands)
        {
            var method = DeclaredMethod(command);
            CallsOfficerGate(method).ShouldBeTrue($"/lottery {command} must call IsOfficer before it acts");
        }
    }

    [Fact]
    public void TheMemberCommandsDoNotCheckTheOfficerGate()
    {
        // Guessing and the reminder toggle are member actions: the FC gate lives in the lottery service, so a member
        // command that started requiring the officer gate would silently lock the FC out.
        foreach (var command in MemberCommands)
        {
            var method = DeclaredMethod(command);
            CallsOfficerGate(method).ShouldBeFalse($"/lottery {command} is a member command");
        }
    }

    [Fact]
    public void TheReminderOptOutButtonIsAComponentHandlerOnTheSameModule()
    {
        var handler = typeof(LotteryInteraction)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(method => method.GetCustomAttribute<ComponentInteractionAttribute>() is not null
                                       && method.GetCustomAttribute<ComponentInteractionAttribute>()!.CustomId
                                       == LotteryReminderComponents.OptOutCustomId);

        handler.ShouldNotBeNull();
        handler!.Name.ShouldBe(nameof(LotteryInteraction.TurnOffReminders));

        // The button belongs to every member, so it must not carry the officer gate.
        CallsOfficerGate(handler).ShouldBeFalse();
    }

    private static MethodInfo DeclaredMethod(string commandName)
    {
        var method = typeof(LotteryInteraction)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(candidate =>
                candidate.GetCustomAttribute<SlashCommandAttribute>()?.Name == commandName);

        method.ShouldNotBeNull($"/lottery {commandName} is not declared on {nameof(LotteryInteraction)}");
        return method!;
    }

    /// <summary>
    ///     Scans the method body for a call to <see cref="DiscordSocketExtensions.IsOfficer(global::Discord.WebSocket.SocketGuildUser, global::ExcelBotCs.Models.Config.DiscordBotOptions)" />.
    ///     The interaction context needs a live Discord socket, so this is the only way to observe the gate without a
    ///     guild; it asserts the call exists, not how the reply reads. An <c>async</c> command's code lives in its
    ///     compiler-generated state machine, so that body is scanned instead of the stub.
    /// </summary>
    private static bool CallsOfficerGate(MethodInfo method)
    {
        return MethodBodies(method).Any(ContainsOfficerGateCall);
    }

    /// <summary>The method's own body, or the <c>MoveNext</c> of its async state machine when it is an <c>async</c> method.</summary>
    private static IEnumerable<MethodBase> MethodBodies(MethodInfo method)
    {
        var stateMachine = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        var moveNext = stateMachine?.GetMethod("MoveNext",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (moveNext is not null)
        {
            yield return moveNext;
            yield break;
        }

        yield return method;
    }

    private static bool ContainsOfficerGateCall(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null) return false;

        for (var index = 0; index < il.Length - 4; index++)
        {
            // call (0x28) and callvirt (0x6F) are the two encodings followed by a 4-byte method token.
            if (il[index] is not (0x28 or 0x6F)) continue;

            var token = BitConverter.ToInt32(il, index + 1);
            if (token <= 0) continue;

            MethodBase? target;
            try
            {
                target = method.Module.ResolveMethod(token);
            }
            catch (ArgumentException)
            {
                // A byte that merely looks like an opcode: not a method token.
                continue;
            }

            if (target?.DeclaringType == typeof(DiscordSocketExtensions) &&
                target.Name == nameof(DiscordSocketExtensions.IsOfficer))
                return true;
        }

        return false;
    }
}
