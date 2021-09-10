namespace Werewolf.Theme.Default.Stages
{
    public class AfternoonStage : Stage
    {
        public override string LanguageId => "afternoon";

        public override string BackgroundId =>
            $"/content/img/stage/{typeof(DefaultTheme).FullName}/background-nightfall.png";

        public override string ColorTheme => "#34a3fe";
    }
}
