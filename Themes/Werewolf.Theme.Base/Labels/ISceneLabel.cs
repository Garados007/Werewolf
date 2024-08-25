namespace Werewolf.Theme.Labels;

public interface ISceneLabel : ILabel
{
    void OnAttachScene(GameRoom game, ISceneLabel label, Scene target);

    void OnDetachScene(GameRoom game, ISceneLabel label, Scene target);
}
