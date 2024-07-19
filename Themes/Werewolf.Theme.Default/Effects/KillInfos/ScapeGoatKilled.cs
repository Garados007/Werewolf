using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.KillInfos;

/// <summary>
/// This is used when the <see cref="Roles.ScapeGoat" /> was killed during <see
/// cref="Phases.DailyVictimElectionPhase" /> by the village. The <see cref="Roles.ScapeGoat" />
/// will now get its revenge.
/// </summary>
public class ScapeGoatKilled : KillInfoEffect
{
    public override string NotificationId => "scapegoat-kill";
}
