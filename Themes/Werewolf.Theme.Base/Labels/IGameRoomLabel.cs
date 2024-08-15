namespace Werewolf.Theme.Labels;

public interface IGameRoomLabel : ILabel
{
    void OnAttachGameRoom(GameRoom game, IGameRoomLabel label);

    void OnDetachGameRoom(GameRoom game, IGameRoomLabel label);
}
