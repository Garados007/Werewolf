namespace Werewolf.Theme.Labels;

public interface IPhaseLabel : ILabel
{
    void OnAttachPhase(GameRoom game, Phase target);

    void OnDetachPhase(GameRoom game, Phase target);
}
