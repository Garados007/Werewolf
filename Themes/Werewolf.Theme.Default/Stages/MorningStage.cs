﻿namespace Werewolf.Theme.Default.Stages
{
    public class MorningStage : Stage
    {
        public override string LanguageId => "morning";

        public override string BackgroundId =>
            $"/content/games/werwolf/img/{typeof(DefaultTheme).FullName}/background-sunrising.png";

        public override string ColorTheme => "#34a3fe";
    }
}
