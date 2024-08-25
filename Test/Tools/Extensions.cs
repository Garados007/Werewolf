using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Werewolf.Theme;
using Werewolf.Theme.Labels;
using Werewolf.User;

namespace Test.Tools;

public static class Extensions
{
    private static string Prefix(bool value)
    => value ? "" : "not ";

    private static GameUserEntry GetUserWithCharacter<TRole>(GameRoom room, int index = 0, Func<TRole, bool>? selector = null)
        where TRole : Character
    {
        selector ??= x => true;
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        var entry = room.Users
            .Select(x => x.Value)
            .Where(x => x.Character is TRole role && selector(role))
            .Skip(index)
            .FirstOrDefault();
        IsNotNull(entry, $"Character {typeof(TRole).FullName} at index {index} and specified rules not found");
        return entry;
    }

    [return: NotNull]
    public static TChar GetCharacter<TChar>(this GameRoom room, int index = 0, Func<TChar, bool>? selector = null)
        where TChar : notnull, Character
    {
        var character = GetUserWithCharacter(room, index, selector)?.Character as TChar;
        IsNotNull(character, $"Character {typeof(TChar).Name} at index {index} not found");
        return character;
    }

    public static string? Vote(this Werewolf.Theme.Voting voting, GameRoom game, Character voter, Character target)
    {
        var voterId = game.TryGetId(voter);
        IsNotNull(voterId, $"Character {voter.Name} has no id");
        var targetId = voting.Options.Where(x => x.option is CharacterOption opt && opt.Character == target)
            .Select(x => x.id)
            .ToArray();
        AreEqual(1, targetId.Length, $"Target {target.Name} is not part of the voting");
        return voting.Vote(game, voterId.Value, targetId[0]);
    }

    public static string? Vote(this Werewolf.Theme.Voting voting, GameRoom game, Character voter, int target)
    {
        var voterId = game.TryGetId(voter);
        IsNotNull(voterId, $"Character {voter.Name} has no id");
        return voting.Vote(game, voterId.Value, target);
    }

    public static string? Vote<TTarget>(this Werewolf.Theme.Voting voting, GameRoom game, Character voter)
        where TTarget : VoteOption
    {
        var voterId = game.TryGetId(voter);
        IsNotNull(voterId, $"Character {voter.Name} has no id");
        var targetId = voting.Options.Where(x => x.option is TTarget)
            .Select(x => x.id)
            .ToArray();
        AreEqual(1, targetId.Length, $"Target {typeof(TTarget).Name} is not part of the voting");
        return voting.Vote(game, voterId.Value, targetId[0]);
    }

    public static void CannotVote(this Voting voting, GameRoom game, Character voter, Character target)
    {
        var voterId = game.TryGetId(voter);
        if (voterId is null)
            return;
        var targetId = voting.Options.Where(x => x.option is CharacterOption opt && opt.Character == target)
            .Select(x => x.id)
            .ToArray();
        if (targetId.Length != 1)
            return;
        IsNotNull(voting.Vote(game, voterId.Value, targetId[0]), "A vote could be made but it was expected that it wouldn't work");
    }

    public static void ExpectNextPhase<TPhase>(this GameRoom room)
        where TPhase : Scene
    {
        room.NextScene();
        IsInstanceOfType<TPhase>(room.Phase);
    }

    public static TVoting ExpectVoting<TVoting>(this GameRoom game, int offset = 0, Func<TVoting, bool>? verifier = null)
        where TVoting : Voting
    {
        verifier ??= x => true;
        var voting = game.Votings
            .Where(x => x is TVoting)
            .Cast<TVoting>()
            .Where(verifier)
            .Skip(offset)
            .FirstOrDefault();
        IsNotNull(voting, $"It was expected that a voting {typeof(TVoting).FullName} (offset: {offset}) exists but not found.");
        return voting;
    }

    public static void ExpectNoVoting<TVoting>(this GameRoom game, int offset = 0, Func<TVoting, bool>? verifier = null)
        where TVoting : Voting
    {
        verifier ??= x => true;
        var voting = game.Votings
            .Where(x => x is TVoting)
            .Cast<TVoting>()
            .Where(verifier)
            .Skip(offset)
            .FirstOrDefault();
        IsNull(voting, $"It was expected that a voting {typeof(TVoting).FullName} (offset: {offset}) doesn't exists but it was found.");
    }

    [Obsolete("use Assert.IsTrue()", true)]
    public static void ExpectLiveState(this Character role, bool alive = true)
    {
        if (role.Enabled != alive)
            throw new InvalidOperationException(
                $"The role {role.GetType().FullName} was expected to be {Prefix(alive)}alive but is {Prefix(role.Enabled)}alive."
            );
    }

    public static void ExpectLabel<THostLabel, TLabel>(this ILabelHost<THostLabel> host, int offset = 0, Func<TLabel, bool>? filter = null)
        where THostLabel : class, ILabel
        where TLabel : THostLabel
    {
        var label = host.Labels.GetEffects<TLabel>()
            .Where(filter ?? (_ => true))
            .Skip(offset)
            .FirstOrDefault();
        IsNotNull(label, $"The host {host.GetType().FullName} was expected to have a label {typeof(TLabel).Name} but hasn't.");
    }

    public static void ExpectNoLabel<THostLabel, TLabel>(this ILabelHost<THostLabel> host, int offset = 0, Func<TLabel, bool>? filter = null)
        where THostLabel : class, ILabel
        where TLabel : THostLabel
    {
        var label = host.Labels.GetEffects<TLabel>()
            .Where(filter ?? (_ => true))
            .Skip(offset)
            .FirstOrDefault();
        IsNull(label, $"The host {host.GetType().FullName} was expected to have no label {typeof(TLabel).Name} but found.");
    }

    public static void ExpectWinner(this GameRoom game, params Character[] winners)
    {
        IsTrue(game.Winner.HasValue);
        var currentWinner = game.Winner.Value.winner;
        var cw = currentWinner.ToArray().ToList()
            .Select(x => game.TryGetRole(x))
            .Where(x => x != null)
            .Select(x => x!.Name);
        var suffix = $"Current winner: {{{string.Join(", ", cw)}}}";
        var all = new HashSet<UserId>();
        foreach (var winner in winners)
        {
            var id = game.TryGetId(winner);
            IsTrue(id.HasValue, $"Character {winner.Name} was expected to have an id. {suffix}");
            _ = all.Add(id.Value);
            var found = false;
            foreach (var w in currentWinner.Span)
                found |= w == id.Value;
            IsTrue(found, $"Character {winner.Name} was expected to be a winner. {suffix}");
        }
        foreach (var current in currentWinner.Span)
        {
            IsTrue(all.Contains(current), $"The user {current} was marked as winner but this wasn't expected. {suffix}");
        }
    }

    public static void ExpectVisibility<TExpectedRole>(this Character victim, GameRoom game, Character viewer)
        where TExpectedRole : Character
    {
        var shownRole = victim.ViewRole(game, viewer);
        IsTrue(
            shownRole.IsAssignableTo(typeof(TExpectedRole)),
            $"It was expected that {victim.GetType().FullName} is shown to {viewer.GetType().FullName} " +
            $"as {typeof(TExpectedRole).FullName} but it is {shownRole.GetType().FullName}."
        );
    }

    public static void ExpectSequence<TSequence>(this GameRoom game)
        where TSequence : Sequence
    {
        IsTrue(game.Sequences.Any(x => x is TSequence), $"Expect sequence {typeof(TSequence).Name}");
    }

    public static Type? GetSeenRole(this Character target, GameRoom game, Character viewer)
    {
        var viewerId = game.TryGetId(viewer);
        var targetId = game.TryGetId(target);
        IsTrue(viewerId.HasValue);
        IsTrue(targetId.HasValue);
        IsTrue(game.Users.TryGetValue(viewerId.Value, out var viewerEntry));
        return Character.GetSeenRole(game, null, viewerEntry.User, targetId.Value, target);
    }
}
