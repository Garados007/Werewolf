using Werewolf.User;
using Werewolf.Theme.Effects;

namespace Werewolf.Theme;

/// <summary>
/// The basic role every game user has. Any special state is encoded in this role.
/// </summary>
public abstract class Character
{
    public EffectCollection<IRoleEffect> Effects { get; } = new();

    private bool enabled = true;
    public bool Enabled
    {
        get => enabled;
        private set
        {
            enabled = value;
            SendRoleInfoChanged();
        }
    }

    public bool HasKillFlag => Effects.GetEffect<KillInfoEffect>() is not null;

    private bool isMajor;
    public bool IsMajor
    {
        get => isMajor;
        set
        {
            isMajor = value;
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
        if (!Enabled)
            yield return "not-alive";
        if (IsMajor)
            yield return "major";
        foreach (var effect in Effects.GetEffects())
            foreach (var tag in effect.GetSeenTags(game, this, viewer))
                yield return tag;
    }

    public void SendRoleInfoChanged()
    {
        Theme.Game?.SendEvent(new Events.OnRoleInfoChanged(this));
    }

    public GameMode Theme { get; }

    public Character(GameMode theme)
    {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public abstract bool? IsSameFaction(Character other);

    public virtual Character ViewRole(Character viewer)
    {
        return Theme.GetBasicRole();
    }

    public abstract Character CreateNew();

    public abstract string Name { get; }

    /// <summary>
    /// Add the kill effect to the list. Mark this as to be killed somewhere in the future.
    /// </summary>
    /// <param name="info">the kill info</param>
    public void AddKillFlag(KillInfoEffect info)
    {
        if (!Enabled)
            return;
        Effects.Add(info);
    }

    /// <summary>
    /// Remove any kill effects. This role wont be killed so far.
    /// </summary>
    public void RemoveKillFlag()
    {
        Effects.Remove<KillInfoEffect>();
    }

    /// <summary>
    /// Mark this role as killed. This will also remove any kill flags. If no kill flags were
    /// attached nothing will happen.
    /// </summary>
    public void ChangeToKilled()
    {
        if (Effects.Remove<KillInfoEffect>() > 0)
            Enabled = false;
    }

    public static Character? GetSeenRole(GameRoom game, uint? round, UserInfo user, UserId targetId, Character target)
    {
        var ownRole = game.TryGetRole(user.Id);
        return (game.Leader == user.Id && !game.LeaderIsPlayer) ||
                targetId == user.Id ||
                round == game.ExecutionRound ||
                (game.AllCanSeeRoleOfDead && !target.Enabled) ||
                (ownRole != null && game.DeadCanSeeAllRoles && !ownRole.Enabled) ?
            target :
            ownRole != null ?
            target.ViewRole(ownRole) :
            null;
    }

    public static IEnumerable<string> GetSeenTags(GameRoom game, UserInfo user, Character? viewer, Character target)
    {
        if (viewer == null && game.Leader != user.Id)
            return Enumerable.Empty<string>();
        if (viewer != null && game.DeadCanSeeAllRoles && !viewer.Enabled)
            viewer = null;
        return target.GetTags(game, viewer);
    }
}
