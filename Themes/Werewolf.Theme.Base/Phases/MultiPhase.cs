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
        readonly TPhase1 phase1 = new TPhase1();
        readonly TPhase2 phase2 = new TPhase2();

        protected TPhase1 Phase1 => phase1;
        protected TPhase2 Phase2 => phase2;

        public override bool CanExecute(GameRoom game)
            => phase1.CanExecute(game) || phase2.CanExecute(game);

        public override async Task InitAsync(GameRoom game)
        {
            await base.InitAsync(game);
            await phase1.InitAsync(game);
            await phase2.InitAsync(game);
        }

        public override bool IsGamePhase => phase1.IsGamePhase || phase2.IsGamePhase;

        public override IEnumerable<Voting> Votings =>
            base.Votings.Concat(phase1.Votings).Concat(phase2.Votings);

        public override void RemoveVoting(Voting voting)
        {
            base.RemoveVoting(voting);
            phase1.RemoveVoting(voting);
            phase2.RemoveVoting(voting);
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            base.ExecuteMultipleWinner(voting, game);
            phase1.ExecuteMultipleWinner(voting, game);
            phase2.ExecuteMultipleWinner(voting, game);
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return Phase1.CanMessage(game, role) || Phase2.CanMessage(game, role);
        }
    }
}
