using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Werewolf.Theme;
using Werewolf.User;

namespace Test.Tools
{
    public static class Extensions
    {
        public static GameUserEntry GetUserWithRole<TRole>(this GameRoom room, int index = 0, Func<TRole, bool>? selector = null)
            where TRole : Role
        {
            selector ??= x => true;
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            var entry = room.Users
                .Select(x => x.Value)
                .Where(x => x.Role is TRole role && selector(role))
                .Skip(index)
                .FirstOrDefault();
            if (entry is null)
                throw new KeyNotFoundException(
                    $"Role {typeof(TRole).FullName} at index {index} and specified rules not found"
                );
            return entry;
        }

        public static TRole GetRole<TRole>(this GameRoom room, int index = 0, Func<TRole, bool>? selector = null)
            where TRole : Role
        {
            var entry = GetUserWithRole<TRole>(room, index, selector);
            if (entry?.Role is TRole role)
                return role;
            else 
                throw new KeyNotFoundException(
                    $"Role {typeof(TRole).FullName} at index {index} and specified rules not found"
                );
        }

        public static string? Vote(this Werewolf.Theme.Votings.PlayerVotingBase voting, GameRoom room, UserId voter, UserId target)
        {
            var optId = voting.GetOptionIndex(target);
            if (optId is null)
                throw new KeyNotFoundException($"cannot find target {target}");
            return voting.Vote(room, voter, optId.Value);
        }

        public static string? Vote(this Werewolf.Theme.Votings.PlayerVotingBase voting, GameRoom room, GameUserEntry voter, GameUserEntry target)
        {
            return Vote(voting, room, voter.User.Id, target.User.Id);
        }

        public static void ExpectPhase<TPhase>(this GameRoom room, Func<TPhase, bool>? verifier = null)
            where TPhase : Phase
        {
            verifier ??= x => true;
            if (room.Phase?.Current is not TPhase @phase || !verifier(@phase))
                throw new InvalidOperationException(
                    $"The current phase is expected to be {typeof(TPhase).FullName} but is " + 
                    $"{room.Phase?.Current?.GetType().FullName ?? "null"}."
                );
        }

        public static async Task ExpectNextPhaseAsync<TPhase>(this GameRoom room, Func<TPhase, bool>? verifier = null)
            where TPhase : Phase
        {
            await room.NextPhaseAsync().ConfigureAwait(false);
            ExpectPhase<TPhase>(room, verifier);
        }

        public static TVoting ExpectVoting<TVoting>(this Phase phase, int offset = 0, Func<TVoting, bool>? verifier = null)
            where TVoting : Voting
        {
            verifier ??= x => true;
            var voting = phase.Votings
                .Where(x => x is TVoting)
                .Cast<TVoting>()
                .Where(verifier)
                .Skip(offset)
                .FirstOrDefault();
            if (voting is null)
                throw new InvalidOperationException(
                    $"It was expected that a voting {typeof(TVoting).FullName} (offset: {offset}) exists but not found."
                );
            return voting;
        }

        public static TVoting ExpectVoting<TVoting>(this GameRoom room, int offset = 0, Func<TVoting, bool>? verifier = null)
            where TVoting : Voting
        {
            var phase = room.Phase?.Current;
            if (phase is null)
                throw new InvalidOperationException(
                    $"A phase with the voting {typeof(TVoting).FullName} was expected but they are no phases right now"
                );
            return phase.ExpectVoting(offset, verifier);
        }

        public static void ExpectNoVoting<TVoting>(this Phase phase, int offset = 0, Func<TVoting, bool>? verifier = null)
            where TVoting : Voting
        {
            verifier ??= x => true;
            var voting = phase.Votings
                .Where(x => x is TVoting)
                .Cast<TVoting>()
                .Where(verifier)
                .Skip(offset)
                .FirstOrDefault();
            if (voting is not null)
                throw new InvalidOperationException(
                    $"It was expected that a voting {typeof(TVoting).FullName} (offset: {offset}) doesn't exists but it was found."
                );
        }

        public static void ExpectNoVoting<TVoting>(this GameRoom room, int offset = 0, Func<TVoting, bool>? verifier = null)
            where TVoting : Voting
        {
            var phase = room.Phase?.Current;
            if (phase is null)
                throw new InvalidOperationException(
                    $"A phase without the voting {typeof(TVoting).FullName} was expected but they are no phases right now"
                );
            phase.ExpectNoVoting(offset, verifier);
        }

        public static void ExpectLiveState(this Role role, KillState state = KillState.Alive)
        {
            if (role.KillState != state)
                throw new InvalidOperationException(
                    $"The role {role.GetType().FullName} was expected to be {state} but is {role.KillState}."
                );
        }

        public static void ExpectTag(this Role role, GameRoom game, string tag)
        {
            if (!role.GetTags(game, null).Contains(tag))
                throw new InvalidOperationException(
                    $"The role {role.GetType().FullName} was expected to have the tag {tag} but hasn't."
                );
        }

        public static void ExpectStage<TStage>(this GameRoom game, Func<TStage, bool>? verifier = null)
        {
            verifier ??= x => true;
            if (game.Phase?.CurrentStep.Stage is not TStage stage || !verifier(stage))
                throw new InvalidOperationException(
                    $"It was expected that the current stage is {typeof(TStage).FullName} but it is " +
                    $"{game.Phase?.CurrentStep.Stage.GetType().FullName ?? "null"}"
                );
        }

        public static void ExpectWinner(this GameRoom game, Func<Role, bool>? verifier = null)
        {
            verifier ??= x => true;
            if (game.Winner is null)
                throw new InvalidOperationException(
                    $"The game is not finished but it was expected"
                );
            foreach (var id in game.Winner!.Value.winner.Span)
            {
                if (!game.Users.TryGetValue(id, out GameUserEntry? entry))
                    throw new InvalidOperationException(
                        $"They winner {id} has no matching user"
                    );
                if (entry.Role is null)
                    throw new InvalidOperationException(
                        $"The winner {id} has no associated role"
                    );
                if (!verifier(entry.Role))
                    throw new InvalidOperationException(
                        $"The winner {id} is invalid"
                    );
            }
        }

        public static void ExpectWinner(this GameRoom game, int winnerCount, Func<Role, bool>? verifier = null)
        {
            ExpectWinner(game, verifier);
            if (game.Winner!.Value.winner.Length != winnerCount)
                throw new InvalidOperationException(
                    $"It was expected to have {winnerCount} but they are {game.Winner.Value.winner.Length} winner."
                );
        }

        public static void ExpectWinner(this GameRoom game, params Role[] winner)
        {
            ExpectWinner(game, winner.Length, role => winner.Contains(role));
        }

        public static void ExpectWinner(this GameRoom game, params GameUserEntry[] winner)
        {
            ExpectWinner(game, winner.Length, role => winner.Any(x => x.Role == role));
        }

        public static void ExpectVisibility<TExpectedRole>(this Role victim, Role viewer)
            where TExpectedRole : Role
        {
            var shownRole = victim.ViewRole(viewer);
            if (shownRole is not TExpectedRole)
                throw new InvalidOperationException(
                    $"It was expected that {victim.GetType().FullName} is shown to {viewer.GetType().FullName} " +
                    $"as {typeof(TExpectedRole).FullName} but it is {shownRole.GetType().FullName}."
                );
        }
    }
}