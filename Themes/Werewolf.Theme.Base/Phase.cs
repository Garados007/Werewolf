using Werewolf.Theme.Labels;

namespace Werewolf.Theme;

public abstract class Phase : ILabelHost<IPhaseLabel>
{
    public List<Scene> EnabledScenes { get; } = [];

    public int SceneIndex { get; private set; } = -1;

    public Scene? CurrentScene => SceneIndex < 0 || SceneIndex >= EnabledScenes.Count ? null : EnabledScenes[SceneIndex];

    /// <summary>
    /// Navigates to the next available scene. Returns false if no scene is available for the
    /// current phase. This methods does also the initialization and the exit of each scene.
    /// </summary>
    /// <param name="game">the current game</param>
    /// <returns>true if a scene is available</returns>
    public bool NextScene(GameRoom game)
    {
        // check if already in a scene
        if (SceneIndex >= 0)
        {
            EnabledScenes[SceneIndex].Exit(game);
        }
        for (SceneIndex++; SceneIndex < EnabledScenes.Count; SceneIndex++)
        {
            Serilog.Log.Verbose("Core: Check scene {name}", EnabledScenes[SceneIndex].LanguageId);
            if (EnabledScenes[SceneIndex].CanExecute(game))
            {
                EnabledScenes[SceneIndex].Init(game);
                return true;
            }
        }
        SceneIndex = -1;
        return false;
    }

    public LabelCollection<IPhaseLabel> Labels { get; } = new();

    public abstract string LanguageId { get; }

    public abstract string BackgroundId { get; }

    public abstract string ColorTheme { get; }

    public abstract Phase? Next(GameRoom game);
}
