namespace Werewolf.Game.Events;

public sealed class SubmitGameEvents(GameEvent gameEvent, GameRoom room, UserInfo user) : EventBase
{
    public GameEvent GameEvent { get; } = gameEvent;

    public GameRoom Room { get; } = room;

    public UserInfo User { get; } = user;

    public override void WriteJson(Utf8JsonWriter writer)
    {
        GameEvent.Write(writer, Room, User);
    }

    protected override void WriteJsonContent(Utf8JsonWriter writer)
    {
    }

    public override void ReadJsonContent(JsonElement json)
    {
        throw new System.NotSupportedException();
    }
}
