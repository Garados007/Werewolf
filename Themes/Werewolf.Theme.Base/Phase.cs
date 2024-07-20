namespace Werewolf.Theme;

public abstract class Phase
{
    public List<Scene> EnabledScenes { get; } = [];

    public int SceneIndex { get; private set; } = -1;

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
            if (EnabledScenes[SceneIndex].CanExecute(game))
            {
                EnabledScenes[SceneIndex].InitAsync(game).Wait();
                return true;
            }
        }
        SceneIndex = -1;
        return false;
    }

    public Phase? Next { get; set; }

    public Labels.LabelCollection<Labels.IPhaseLabel> Labels { get; } = new();

    public abstract string LanguageId { get; }

    public abstract string BackgroundId { get; }

    public abstract string ColorTheme { get; }
}
