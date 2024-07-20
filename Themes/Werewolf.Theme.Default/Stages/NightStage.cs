namespace Werewolf.Theme.Default.Stages;

public class NightStage : Phase
{
    public override string LanguageId => "night";

    public override string BackgroundId =>
        $"/content/img/stage/{typeof(DefaultTheme).FullName}/background-night.png";

    public override string ColorTheme => "#000911";
}
