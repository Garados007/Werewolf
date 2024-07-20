namespace Werewolf.Theme.Labels;

public interface ISceneLabel : ILabel
{
    void OnAttachScene(GameRoom game, Scene target);

    void OnDetachScene(GameRoom game, Scene target);
}
