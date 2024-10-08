﻿using System.Diagnostics.CodeAnalysis;
using Werewolf.User;

namespace Werewolf.Theme;

public abstract class GameMode
{
    public string LanguageTheme { get; set; } = "default";

    public abstract Phase? GetStartPhase(GameRoom game);

    public abstract IEnumerable<WinConditionCheck> GetWinConditions();

    public GameRoom? Game { get; }

    public UserFactory Users { get; }

    public GameMode(GameRoom? game, UserFactory users)
        => (Game, Users) = (game, users ?? throw new ArgumentNullException(nameof(users)));

    public virtual bool CheckRoleUsage(string character, ref int count, int oldCount,
        [NotNullWhen(false)] out string? error
    )
    {
        if (count < 0)
        {
            error = "invalid number of roles (require >= 0)";
            count = oldCount;
            return false;
        }
        if (count > 500)
        {
            error = "invalid number of roles (require <= 500)";
            count = oldCount;
            return false;
        }
        error = null;
        return true;
    }

    public virtual void PostInit(GameRoom game)
    {

    }

    public abstract IEnumerable<string> GetCharacterNames();

    public abstract Character? CreateCharacter(string name);

    public abstract string? GetCharacterName(Type type);

    public abstract ReadOnlySpan<Type> GetEvents();

    public abstract bool IsEnabled();
}
