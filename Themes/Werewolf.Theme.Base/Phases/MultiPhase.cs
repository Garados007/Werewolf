using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Werewolf.Theme.Phases
{
    /// <summary>
    /// This phase binds the logic of two phases and make them available as one
    /// </summary>
    /// <typeparam name="TPhase1">the logic of the first part</typeparam>
    /// <typeparam name="TPhase2">the logic of the second part</typeparam>
    public abstract class MultiPhase<TPhase1, TPhase2> : Phase
        where TPhase1 : Phase, new()
        where TPhase2 : Phase, new()
    {
        protected TPhase1 Phase1 { get; } = new TPhase1();
        protected TPhase2 Phase2 { get; } = new TPhase2();

        public override bool CanExecute(GameRoom game)
            => Phase1.CanExecute(game) || Phase2.CanExecute(game);

        public override async Task InitAsync(GameRoom game)
        {
            await base.InitAsync(game).ConfigureAwait(false);
            if (Phase1.CanExecute(game))
                await Phase1.InitAsync(game).ConfigureAwait(false);
            if (Phase2.CanExecute(game))
                await Phase2.InitAsync(game).ConfigureAwait(false);
        }

        public override bool IsGamePhase => Phase1.IsGamePhase || Phase2.IsGamePhase;

        public override IEnumerable<Voting> Votings =>
            base.Votings.Concat(Phase1.Votings).Concat(Phase2.Votings);

        public override void RemoveVoting(Voting voting)
        {
            base.RemoveVoting(voting);
            Phase1.RemoveVoting(voting);
            Phase2.RemoveVoting(voting);
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            base.ExecuteMultipleWinner(voting, game);
            Phase1.ExecuteMultipleWinner(voting, game);
            Phase2.ExecuteMultipleWinner(voting, game);
        }

        public override bool CanMessage(GameRoom game, RoleKind role)
        {
            return Phase1.CanMessage(game, role) || Phase2.CanMessage(game, role);
        }
    }
}
