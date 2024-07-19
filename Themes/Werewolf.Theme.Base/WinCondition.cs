using System.Diagnostics.CodeAnalysis;

namespace Werewolf.Theme;

public delegate bool WinConditionCheck(GameRoom game, [NotNullWhen(true)] out ReadOnlyMemory<Role>? winner);

public class WinCondition
{
    public bool Check(GameRoom game, [NotNullWhen(true)] out ReadOnlyMemory<Role>? winner)
    {
        // first execute the theme win conditions. These are expected to return faster
        if (game.Theme != null)
            foreach (var condition in game.Theme.GetWinConditions())
                if (condition(game, out winner))
                    return true;
        // second execute our win conditions
        foreach (var condition in GetConditions())
            if (condition(game, out winner))
                return true;
        // no win condition matches
        winner = null;
        return false;
    }

    private IEnumerable<WinConditionCheck> GetConditions()
    {
        yield return OnlyOneFaction;
    }

    private bool OnlyOneFaction(GameRoom game, [NotNullWhen(true)] out ReadOnlyMemory<Role>? winner)
    {
        static bool IsSameFaction(Role role1, Role role2)
        {
            var check = role1.IsSameFaction(role2);
            if (check == null)
                check = role2.IsSameFaction(role1);
            return check ?? false;
        }

        Span<Role> player = game.AliveRoles.ToArray();
        for (int i = 0; i < player.Length; ++i)
            for (int j = i + 1; j < player.Length; ++j)
                if (!IsSameFaction(player[i], player[j]))
                {
                    winner = null;
                    return false;
                }

        // game is finished, now get all players that won
        var list = new List<Role>(player.ToArray());
        if (list.Count == 0)
        {
            winner = list.ToArray();
            return true;
        }
        foreach (var role in game.Users.Select(x => x.Value.Role))
            if (role != null && !role.Enabled)
            {
                // check if has same faction with all players
                for (int i = 0; i < player.Length; ++i)
                    if (!IsSameFaction(role, player[i]))
                        // skip current loop and check next one. this goto is faster than the
                        // boolean checks
                        goto continue_next;
                list.Add(role);
            continue_next:;
            }
        winner = list.ToArray();
        return true;
    }
}
