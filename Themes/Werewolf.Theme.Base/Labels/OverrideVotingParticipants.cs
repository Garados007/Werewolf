using System.Collections;
using Werewolf.User;

namespace Werewolf.Theme.Labels;

/// <summary>
/// Overrides the selection of the Participants of <see cref="Votings.PlayerVotingBase" /> a single
/// time. This needs to be attached to <see cref="GameRoom" /> and will be loaded as soon as the
/// specified voting will be created the next time.
/// </summary>
public abstract class OverrideVotingParticipants : IGameRoomEffect, IEnumerable<User.UserId>
{
    public Type Voting { get; }

    public ReadOnlyMemory<User.UserId> Participants { get; }

    public OverrideVotingParticipants(Type voting, ReadOnlyMemory<User.UserId> participants)
    {
        if (!voting.IsAssignableTo(typeof(Votings.PlayerVotingBase)))
            throw new ArgumentException(
                $"type needs to be inherited from {typeof(Voting).FullName}",
                nameof(voting)
            );
        Voting = voting;
        Participants = participants;
    }

    public IEnumerator<UserId> GetEnumerator()
    {
        for (int i = 0; i < Participants.Length; ++i)
            yield return Participants.Span[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();
}

/// <summary>
/// Overrides the selection of the Participants of <typeparamref name="T"/> a single
/// time. This needs to be attached to <see cref="GameRoom" /> and will be loaded as soon as the
/// specified voting will be created the next time.
/// </summary>
/// <typeparam name="T">The voting that needs to be overwritten.</typeparam>
public class OverrideVotingParticipants<T> : OverrideVotingParticipants
    where T : Votings.PlayerVotingBase
{
    public OverrideVotingParticipants(ReadOnlyMemory<User.UserId> participants)
        : base(typeof(T), participants)
    { }

    public OverrideVotingParticipants(IEnumerable<User.UserId> participants)
        : base(typeof(T), participants.ToArray())
    { }
}
