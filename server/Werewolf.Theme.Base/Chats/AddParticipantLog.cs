using Werewolf.User;

namespace Werewolf.Theme.Chats;

public class AddParticipantLog : ChatServiceMessage
{
    public User.UserId User { get; }

    public AddParticipantLog(User.UserId user)
    {
        User = user;
    }

    public override bool Epic => true;

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        yield return ("user", User);
    }
}
