using Werewolf.Theme.Chats;

namespace Werewolf.Theme.Default.Chats;

public class PlayerKillLog : ChatServiceMessage
{
    public User.UserId User { get; }

    public PlayerKillLog(User.UserId user)
        => User = user;

    public override bool Epic => false;

    public override bool CanSendTo(GameRoom game, User.UserInfo user)
        => true;

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        yield return ("user", User);
    }
}