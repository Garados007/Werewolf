using System.Collections.Generic;
using System.Linq;
using Werewolf.Theme;
using Werewolf.Theme.Votings;
using Werewolf.Users.Api;

namespace Avalon.The.Resistance.Phases
{
    public class TeamBuilding : Phase
    {
        public class LeaderVoting : PlayerVotingBase
        {
            public LeaderVoting(GameRoom game, IEnumerable<UserId>? participants = null) 
                : base(game, participants)
            {
            }

            protected override bool AllowDoNothingOption => true;

            protected override string DoNothingOptionTextId => "finish-selection";

            private bool canFinishVoting = false;

            public override bool CanView(Role viewer)
                => true;

            public override bool CanVote(Role voter)
            {
                return voter is BaseRole baseRole && baseRole.IsMissionLeader;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                if (role is BaseRole baseRole)
                    baseRole.IsSelectedByLeader = true;
                game.SendEvent(new Werewolf.Theme.Events.OnRoleInfoChanged(role));
            }

            protected override int GetMissingVotes(GameRoom game)
            {
                return canFinishVoting ? 0 : 1;
            }

            public override string? Vote(GameRoom game, UserId voter, int id)
            {
                if (canFinishVoting)
                    return "You already select finish";

                var option = Options
                    .Where(x => x.id == id)
                    .Select(x => x.option)
                    .FirstOrDefault();

                if (option == null)
                    return "option not found";

                string? error;
                if ((error = Vote(game, voter, option)) != null)
                    return error;

                game.SendEvent(new Werewolf.Theme.Events.SetVotingVote(this, id, voter));

                if (id == 0)
                    canFinishVoting = true;

                CheckVotingFinished(game);

                return null;
            }

            protected override void AfterFinishExecute(GameRoom game)
            {
                game.SendEvent(new Werewolf.Theme.Events.PlayerNotification(
                    "leader-vote",
                    GetResultUserIds().ToArray()
                ));
            }
        }

        public override bool CanExecute(GameRoom game)
            => true;

        public override bool CanMessage(GameRoom game, Role role)
            => true;

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            AddVoting(new LeaderVoting(game));
        }

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            if (voting is LeaderVoting vot)
            {
                foreach (var id in vot.GetResultUserIds())
                    if (game.Participants.TryGetValue(id, out Role? role) && role != null)
                        vot.Execute(game, id, role);
                RemoveVoting(voting);
            }
        }
    }
}