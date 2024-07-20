using Werewolf.Theme.Labels;

namespace Werewolf.Theme.Default.Effects.BeforeKillAction;

public class LogPlayerKill : BeforeKillActionEffect
{
    public override void Execute(GameRoom game, Character current)
    {
        if (game.TryGetId(current) is User.UserId id)
            game.SendChat(new Chats.PlayerKillLog(id));
    }
}
