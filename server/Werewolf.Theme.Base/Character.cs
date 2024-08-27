using Werewolf.User;
using Werewolf.Theme.Labels;

namespace Werewolf.Theme;

/// <summary>
/// The basic role every game user has. Any special state is encoded in this role.
/// </summary>
public abstract class Character : ILabelHost<ICharacterLabel>
{
    public Character(GameMode mode)
    {
        Mode = mode;
        Labels.Added += _ => SendRoleInfoChanged();
        Labels.Removed += _ => SendRoleInfoChanged();
    }

    public LabelCollection<ICharacterLabel> Labels { get; } = new();

    private bool enabled = true;
    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            SendRoleInfoChanged();
        }
    }

    /// <summary>
    /// Get a list of special tags that are defined for this role.
    /// </summary>
    /// <param name="game">The current game</param>
    /// <param name="viewer">The viewer of this role. null for the leader</param>
    /// <returns>a list of defined tags</returns>
    public virtual IEnumerable<string> GetTags(GameRoom game, Character? viewer)
    {
        foreach (var effect in Labels.GetEffects())
            if (effect.CanLabelBeSeen(game, this, viewer))
                yield return effect.Name;
    }

    public void SendRoleInfoChanged()
    {
        Mode.Game?.SendEvent(new Events.OnRoleInfoChanged(this));
    }

    public GameMode Mode { get; }

    public virtual bool? IsSameFaction(Character other) => null;

    /// <summary>
    /// List of all character that can see the true identity of this character regardless of the
    /// usual rules.
    /// </summary>
    public List<Character> Visible { get; } = [];

    public abstract Type ViewRole(GameRoom game, Character viewer);

    public abstract string Name { get; }

    public static Type? GetSeenRole(GameRoom game, uint? round, UserInfo user, UserId targetId, Character target)
    {
        var ownRole = game.TryGetRole(user.Id);
        return  // viewer is gm without a character
                (game.Leader == user.Id && !game.LeaderIsPlayer) ||
                // viewer is the character itself
                targetId == user.Id ||
                // the game is over
                round == game.ExecutionRound ||
                // target is disabled and it is allowed to see details
                (game.AllCanSeeRoleOfDead && !target.Enabled) ||
                // viewer is disabled and is allowed to see details
                (ownRole != null && game.DeadCanSeeAllRoles && !ownRole.Enabled) ||
                // viewer was explicitely whitelisted to see this character
                (ownRole is not null && target.Visible.Contains(ownRole)) ?
            // show real type
            target.GetType() :
            ownRole != null ?
            // let the character decide
            target.ViewRole(game, ownRole) :
            // viewer is a guest
            null;
    }

    public static IEnumerable<string> GetSeenTags(GameRoom game, UserInfo user, Character? viewer, Character target)
    {
        // check if viewer is just a guest
        if (viewer == null && game.Leader != user.Id)
            return [];
        // let disabled player see the same as the gm
        if (viewer != null && game.DeadCanSeeAllRoles && !viewer.Enabled)
            viewer = null;
        return target.GetTags(game, viewer);
    }

    public virtual void Init(GameRoom game) { }
}
