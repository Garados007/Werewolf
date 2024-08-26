using Werewolf.User;

namespace Werewolf.Theme.Chats;

public class GameEndLog : ChatServiceMessage
{
    public override bool Epic => false;

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        return Enumerable.Empty<(string key, ChatVariable value)>();
    }
}