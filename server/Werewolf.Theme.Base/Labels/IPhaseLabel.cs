namespace Werewolf.Theme.Labels;

public interface IPhaseLabel : ILabel
{
    void OnAttachPhase(GameRoom game, IPhaseLabel label, Phase target);

    void OnDetachPhase(GameRoom game, IPhaseLabel label, Phase target);
}
