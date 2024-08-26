using Werewolf.Theme.Labels;

namespace Werewolf.Theme;

public abstract class Scene : ILabelHost<ISceneLabel>
{

    public LabelCollection<ISceneLabel> Labels { get; } = new();

    public virtual string LanguageId
    {
        get
        {
            var name = GetType().FullName ?? "";
            var ind = name.LastIndexOf('.');
            return ind >= 0 ? name[(ind + 1)..] : name;
        }
    }

    public abstract bool CanExecute(GameRoom game);

    public abstract bool CanMessage(GameRoom game, Character role);

    public virtual void Init(GameRoom game)
    {
        game.ClearVotings();
    }

    public virtual void Exit(GameRoom game) { }
}
