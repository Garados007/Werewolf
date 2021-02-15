using Werewolf.Users.Api;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class ElectMajorPhase : Phase, IDayPhase<ElectMajorPhase>
    {
        public class ElectMajor : PlayerVotingBase
        {
            public ElectMajor(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
            }


            public override bool CanView(Role viewer)
            {
                return true;
            }

            public override bool CanVote(Role voter)
            {
                return voter.IsAlive;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                role.IsMajor = true;
                game.SendEvent(new Events.PlayerNotification("new-voted-major", new[] { id }));
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            var isMajorRemoved = game.Participants.Values
                .Where(x => x is Roles.Idiot)
                .Cast<Roles.Idiot>()
                .Where(x => x.WasMajor)
                .Any();
            return !isMajorRemoved && !game.AliveRoles.Where(x => x.IsMajor).Any();
        }

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            AddVoting(new ElectMajor(game));
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            if (voting is ElectMajor em)
            {
                var ids = em.GetResultUserIds().ToArray();
                if (ids.Length > 0)
                    AddVoting(new ElectMajor(game, ids));
                RemoveVoting(voting);
            }
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return true;
        }
    }
}
