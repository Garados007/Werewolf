namespace Werewolf.Theme.Default.Stages;

public class DayStage : Stage
{
    public override string LanguageId => "day";

    public override string BackgroundId =>
        $"/content/img/stage/{typeof(DefaultTheme).FullName}/background-day.png";

    public override string ColorTheme => "#34a3fe";
}
