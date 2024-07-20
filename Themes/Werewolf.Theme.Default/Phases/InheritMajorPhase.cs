using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;

namespace Werewolf.Theme.Default.Phases;

public class InheritMajorPhase : SingleVotingPhase<InheritMajorPhase.InheritMajor>
{
    public class InheritMajor : PlayerVotingBase
    {
        public InheritMajor(GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(game,
                role => role.Enabled && !role.IsMajor
            ))
        {
        }

        public override bool CanView(Character viewer)
        {
            return viewer.IsMajor;
        }

        protected override bool CanVoteBase(Character voter)
        {
            return voter.IsMajor && !voter.Enabled;
        }

        public override void Execute(GameRoom game, UserId id, Character role)
        {
            foreach (var entry in game.Users.Select(x => x.Value.Role))
                if (entry != null)
                    entry.IsMajor = false;
            role.IsMajor = true;
            game.SendEvent(new Events.PlayerNotification(
                "new-major",
                new[] { id }
            ));
        }
    }

    public override bool CanExecute(GameRoom game)
    {
        return game.Users
            .Select(x => x.Value.Role)
            .Where(x => x != null && x.IsMajor && x.Enabled && x.HasKillFlag)
            .Any();
    }

    protected override InheritMajor Create(GameRoom game, IEnumerable<UserId>? ids = null)
        => new(game, ids);

    public override bool CanMessage(GameRoom game, Character role)
    {
        return true;
    }
}
