using Werewolf.User;

namespace Werewolf.Theme.Chats;

public class RemoveParticipantLog : ChatServiceMessage
{
    public User.UserId User { get; }

    public RemoveParticipantLog(User.UserId user)
    {
        User = user;
    }

    public override bool Epic => false;

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        yield return ("user", User);
    }
}