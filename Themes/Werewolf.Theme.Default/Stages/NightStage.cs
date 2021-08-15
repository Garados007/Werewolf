namespace Werewolf.Theme.Default.Stages
{
    public class NightStage : Stage
    {
        public override string LanguageId => "night";

        public override string BackgroundId =>
            $"/content/img/{typeof(DefaultTheme).FullName}/background-night.png";

        public override string ColorTheme => "#000911";
    }
}
