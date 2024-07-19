using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.BeforeKillAction;

public class LogPlayerKill : BeforeKillActionEffect
{
    public override void Execute(GameRoom game, Role current)
    {
        if (game.TryGetId(current) is User.UserId id)
            game.SendChat(new Chats.PlayerKillLog(id));
    }
}
