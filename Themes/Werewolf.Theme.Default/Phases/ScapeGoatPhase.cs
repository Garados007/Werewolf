﻿using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Theme.Default.Effects.KillInfos;

namespace Werewolf.Theme.Default.Phases
{
    public class ScapeGoatPhase : SeperateVotingPhase<ScapeGoatPhase.ScapeGoatSelect, ScapeGoat>
    {
        public class ScapeGoatSelect : PlayerVotingBase
        {
            public ScapeGoat ScapeGoat { get; }

            public ScapeGoatSelect(ScapeGoat scapeGoat, GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
                ScapeGoat = scapeGoat;
            }

            protected override bool AllowDoNothingOption => true;

            protected override string DoNothingOptionTextId => "stop-voting";

            private bool CanFinishVoting;

            public override bool CanView(Role viewer)
            {
                return true;
            }

            protected override bool CanVoteBase(Role voter)
            {
                return voter == ScapeGoat;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                game.Effects.Add(
                    new Werewolf.Theme.Effects.OverrideVotingVoter<Phases.DailyVictimElectionPhase.DailyVote>(
                        (ReadOnlyMemory<UserId>)new[] { id }
                    )
                );
            }

            protected override int GetMissingVotes(GameRoom game)
            {
                return CanFinishVoting ? 0 : 1;
            }

            public override string? Vote(GameRoom game, UserId voter, int id)
            {
                if (CanFinishVoting)
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

                game.SendEvent(new Events.SetVotingVote(this, id, voter));

                if (id == 0)
                    CanFinishVoting = true;

                CheckVotingFinished(game);

                return null;
            }

            protected override void AfterFinishExecute(GameRoom game)
            {
                game.SendEvent(new Events.PlayerNotification(
                    "scapegoat-vote",
                    GetResultUserIds().ToArray()
                ));
                ScapeGoat.TakingRevenge = true;
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            return base.CanExecute(game) &&
                !game.Users
                    .Select(x => x.Value.Role)
                    .Where(x => x is OldMan oldMan && oldMan.WasKilledByVillager)
                    .Any();
        }

        protected override ScapeGoatSelect Create(ScapeGoat role, GameRoom game, IEnumerable<UserId>? ids = null)
            => new ScapeGoatSelect(role, game, ids);

        protected override bool FilterVoter(ScapeGoat role)
            => role.IsAlive && role.Effects.GetEffect<ScapeGoatKilled>() is not null
            && !role.TakingRevenge;

        protected override ScapeGoat GetRole(ScapeGoatSelect voting)
            => voting.ScapeGoat;

        public override void ExecuteMultipleWinner(Voting voting, GameRoom game)
        {
            if (voting is ScapeGoatSelect select)
            {
                game.Effects.Add(
                    new Werewolf.Theme.Effects.OverrideVotingVoter<Phases.DailyVictimElectionPhase.DailyVote>(
                        select.GetResultUserIds()
                    )
                );
                RemoveVoting(voting);
            }
        }

        public override bool CanMessage(GameRoom game, Role role)
        {
            return true;
        }
    }
}
