namespace Werewolf.User;

public abstract class UserConfig
{
    public abstract string Username { get; }

    public abstract ValueTask SetUsernameAsync(string username);

    public abstract string Image { get; }

    public abstract ValueTask SetImageAsync(string image);

    public abstract string ThemeColor { get; }

    public abstract ValueTask SetThemeColorAsync(string themeColor);

    public abstract string? BackgroundImage { get; }

    public abstract ValueTask SetBackgroundImageAsync(string? backgroundImage);

    public abstract string Language { get; }

    public abstract ValueTask SetLanguageAsync(string language);
}
