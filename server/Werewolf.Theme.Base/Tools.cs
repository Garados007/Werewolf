using System.Runtime.InteropServices;
using OneOf;
using OneOf.Types;
using Werewolf.Theme.Labels;

namespace Werewolf.Theme;

public static class Tools
{
    private static readonly ThreadLocal<Random> rng = new(() => new Random());

    public static List<T> AsList<T>(this IEnumerable<T> @enum)
    {
        return @enum is List<T> list ? list : @enum.ToList();
    }

    public static void SetSeed(int seed)
    {
        rng.Value = new Random(seed);
    }

    public static long GetRandom(long max)
    {
        return rng.Value!.NextInt64(max);
    }

    public static List<List<T>> Split<T>(long chunks, IEnumerable<T> col)
    {
        if (chunks <= 1)
            return [col.ToList()];
        int size = (int)Math.Min(chunks, int.MaxValue);
        var result = new List<List<T>>(size);
        for (int i = 0; i < size; ++i)
            result.Add([]);
        var offset = 0;
        foreach (var item in col)
        {
            result[offset].Add(item);
            offset = (offset + 1) % size;
        }
        return result;
    }

    public static List<T> Shuffle<T>(IEnumerable<T> col)
    {
        var source = col.ToList();
        rng.Value!.Shuffle(CollectionsMarshal.AsSpan(source));
        return source;
    }

    public static OneOf<TResult, None> GetField<TLabel, TResult>(Labels.ILabel item, Func<TLabel, TResult> accessor)
        where TLabel : Labels.ILabel
        where TResult : notnull
    {
#pragma warning disable IDE0046 // In bedingten Ausdruck konvertieren
        if (item is TLabel value)
            return accessor(value);
        else return new None();
#pragma warning restore IDE0046 // In bedingten Ausdruck konvertieren
    }

    public static List<T> GetCol<T>(int index, List<List<T>> col)
    {
        return index < 0 || index >= col.Count ? [] : col[index];
    }

    public static List<T> GetCol<T>(int index, IEnumerable<List<T>> col)
    {
        if (index < 0)
            return [];
        foreach (var item in col)
        {
            if (index == 0)
                return item;
            index--;
        }
        return [];
    }

    public static OneOf<T, None> Get<T>(this List<T> col, long index)
    {
        return index < 0 || index >= col.Count || index >= int.MaxValue ?
            new None() : col[(int)index];
    }

    public static OneOf<T, None> Get<T>(this IEnumerable<T> col, long index)
    {
        if (index < 0)
            return new None();
        foreach (var item in col)
        {
            if (index == 0)
                return item;
            index--;
        }
        return new None();
    }

    public static List<T> Rewrap<T>(T item)
    {
        return [item];
    }

    public static IEnumerable<List<T>> Rewrap<T>(IEnumerable<T> col)
    {
        foreach (var item in col)
            yield return [item];
    }

    public static IEnumerable<List<T>> Rewrap<T>(List<T> col)
    {
        foreach (var item in col)
            yield return [item];
    }

    public static ICharacterLabel Add(GameRoom game, Character character, ICharacterLabel label)
    {
        if (character.Labels.Contains(label))
            return label;
        _ = character.Labels.Add(label);
        label.OnAttachCharacter(game, label, character);
        return label;
    }

    public static IPhaseLabel Add(GameRoom game, Phase phase, IPhaseLabel label)
    {
        if (phase.Labels.Contains(label))
            return label;
        _ = phase.Labels.Add(label);
        label.OnAttachPhase(game, label, phase);
        return label;
    }

    public static ISceneLabel Add(GameRoom game, Scene scene, ISceneLabel label)
    {
        if (scene.Labels.Contains(label))
            return label;
        _ = scene.Labels.Add(label);
        label.OnAttachScene(game, label, scene);
        return label;
    }

    public static IVotingLabel Add(GameRoom game, Voting voting, IVotingLabel label)
    {
        if (voting.Labels.Contains(label))
            return label;
        _ = voting.Labels.Add(label);
        label.OnAttachVoting(game, label, voting);
        return label;
    }

    public static IGameRoomLabel Add(GameRoom game, IGameRoomLabel label)
    {
        if (game.Labels.Contains(label))
            return label;
        _ = game.Labels.Add(label);
        return label;
    }

    public static void Remove(GameRoom game, Character character, ICharacterLabel label)
    {
        _ = character.Labels.Remove(label);
        label.OnDetachCharacter(game, label, character);
    }

    public static void Remove(GameRoom game, Phase phase, IPhaseLabel label)
    {
        _ = phase.Labels.Remove(label);
        label.OnDetachPhase(game, label, phase);
    }

    public static void Remove(GameRoom game, Scene scene, ISceneLabel label)
    {
        _ = scene.Labels.Remove(label);
        label.OnDetachScene(game, label, scene);
    }

    public static void Remove(GameRoom game, Voting voting, IVotingLabel label)
    {
        _ = voting.Labels.Remove(label);
        label.OnDetachVoting(game, label, voting);
    }

    public static void Remove(GameRoom game, IGameRoomLabel label)
    {
        _ = game.Labels.Remove(label);
    }

    public static void Remove<T>(GameRoom game, Character character)
        where T : ICharacterLabel
    {
        foreach (var label in character.Labels.GetEffects<T>().ToArray())
            Remove(game, character, label);
    }

    public static void Remove<T>(GameRoom game, Phase phase)
        where T : IPhaseLabel
    {
        foreach (var label in phase.Labels.GetEffects<T>().ToArray())
            Remove(game, phase, label);
    }

    public static void Remove<T>(GameRoom game, Scene scene)
        where T : ISceneLabel
    {
        foreach (var label in scene.Labels.GetEffects<T>().ToArray())
            Remove(game, scene, label);
    }

    public static void Remove<T>(GameRoom game, Voting voting)
        where T : IVotingLabel
    {
        foreach (var label in voting.Labels.GetEffects<T>().ToArray())
            Remove(game, voting, label);
    }

    public static void Remove<T>(GameRoom game)
        where T : IGameRoomLabel
    {
        foreach (var label in game.Labels.GetEffects<T>().ToArray())
            Remove(game, label);
    }

    public static void MakeVisible(GameRoom game, ICharacterLabel target, Character viewer)
    {
        target.Visible.Add(viewer);
        foreach (var targetCharacter in game.AllCharacters)
            if (targetCharacter.Labels.Contains(target))
                game.SendEvent(new Events.OnRoleInfoChanged(targetCharacter));
    }

    public static void MakeVisible(GameRoom game, ICharacterLabel target, IEnumerable<Character> viewer)
    {
        foreach (var view in viewer)
            MakeVisible(game, target, view);
    }

    public static void MakeVisible(GameRoom game, Character target, Character viewer)
    {
        target.Visible.Add(viewer);
        game.SendEvent(new Events.OnRoleInfoChanged(target));
    }

    public static void MakeVisible(GameRoom game, Character target, IEnumerable<Character> viewer)
    {
        foreach (var view in viewer)
            MakeVisible(game, target, view);
    }

    public static void MakeInvisible(GameRoom game, ICharacterLabel target, Character viewer)
    {
        _ = target.Visible.Remove(viewer);
        foreach (var targetCharacter in game.AllCharacters)
            if (targetCharacter.Labels.Contains(target))
                game.SendEvent(new Events.OnRoleInfoChanged(targetCharacter));
    }

    public static void MakeInvisible(GameRoom game, ICharacterLabel target, IEnumerable<Character> viewer)
    {
        foreach (var view in viewer)
            MakeInvisible(game, target, view);
    }

    public static void MakeInvisible(GameRoom game, Character target, Character viewer)
    {
        _ = target.Visible.Remove(viewer);
        game.SendEvent(new Events.OnRoleInfoChanged(target));
    }

    public static void MakeInvisible(GameRoom game, Character target, IEnumerable<Character> viewer)
    {
        foreach (var view in viewer)
            MakeInvisible(game, target, view);
    }

    public static void Cancel(GameRoom game, Voting voting)
    {
        voting.Abort();
        game.RemoveVoting(voting);
    }
}
