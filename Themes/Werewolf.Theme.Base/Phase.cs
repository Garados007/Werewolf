using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Werewolf.Theme
{
    public abstract class Phase
    {
        public virtual string LanguageId
        {
            get
            {
                var name = GetType().FullName ?? "";
                var ind = name.LastIndexOf('.');
                if (ind >= 0)
                    return name[(ind + 1)..];
                else return name;
            }
        }

        public abstract bool CanExecute(GameRoom game);

        public abstract bool CanMessage(GameRoom game, Role role);

        public virtual async Task InitAsync(GameRoom game)
        {
            Init(game);
            await Task.CompletedTask;
        }

        protected virtual void Init(GameRoom game)
        {
            votings.Clear();
            this.game = game;
        }

        public virtual bool IsGamePhase => true;

        readonly List<Voting> votings = new List<Voting>();
        public virtual IEnumerable<Voting> Votings => votings.ToArray();

        private GameRoom? game;

        protected virtual void AddVoting(Voting voting)
        {
            if (!voting.Options.Any())
                return;
            votings.Add(voting);
            voting.Started = game?.AutostartVotings ?? false;
            if (game?.UseVotingTimeouts ?? false)
                voting.SetTimeout(game, true);
            game?.SendEvent(new Events.AddVoting(voting));
        }

        public virtual void RemoveVoting(Voting voting)
        {
            votings.Remove(voting); 
            game?.SendEvent(new Events.RemoveVoting(voting.Id));
        }

        public virtual void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {

        }
    }
}
