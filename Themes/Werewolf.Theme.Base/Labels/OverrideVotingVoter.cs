using System.Collections;
using Werewolf.User;

namespace Werewolf.Theme.Labels;

/// <summary>
/// Overrides the group of user that are eligible to vote for the next voting. This can ignore the
/// default behavior of selecting voter. This needs to be attached to <see cref="GameRoom" /> and
/// will be loaded as soon as the specified voting will be created the next time.
/// </summary>
public abstract class OverrideVotingVoter : IGameRoomEffect, IEnumerable<User.UserId>
{
    public Type Voting { get; }

    public ReadOnlyMemory<User.UserId> Voter { get; }

    public OverrideVotingVoter(Type voting, ReadOnlyMemory<User.UserId> voter)
    {
        if (!voting.IsAssignableTo(typeof(Voting)))
            throw new ArgumentException(
                $"type needs to be inherited from {typeof(Voting).FullName}",
                nameof(voting)
            );
        Voting = voting;
        Voter = voter;
    }

    public IEnumerator<UserId> GetEnumerator()
    {
        for (int i = 0; i < Voter.Length; ++i)
            yield return Voter.Span[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();
}

/// <summary>
/// Overrides the group of user that are eligible to vote for the next voting. This can ignore the
/// default behavior of selecting voter. This needs to be attached to <see cref="GameRoom" /> and
/// will be loaded as soon as the specified voting will be created the next time.
/// </summary>
/// <typeparam name="T">The voting that needs to be overwritten</typeparam>
public class OverrideVotingVoter<T> : OverrideVotingVoter
    where T : Voting
{
    public OverrideVotingVoter(ReadOnlyMemory<User.UserId> voter)
        : base(typeof(T), voter)
    { }

    public OverrideVotingVoter(IEnumerable<User.UserId> voter)
        : base(typeof(T), voter.ToArray())
    { }
}
