using System.Threading.Tasks;
using Werewolf.User;

namespace Test.Tools.User;

public class TestUserConfig : UserConfig
{
    public int Index { get; }

    public TestUserConfig(int index) => Index = index;

    public override string Username => Index.ToString();

    public override string Image => "";

    public override string ThemeColor => "#333333";

    public override string? BackgroundImage => null;

    public override string Language => "en";

    public override ValueTask SetBackgroundImageAsync(string? backgroundImage)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask SetLanguageAsync(string language)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask SetThemeColorAsync(string themeColor)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask SetUsernameAsync(string username)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask SetImageAsync(string image)
    {
        return ValueTask.CompletedTask;
    }
}
