using MaxLib.WebServer;
using MaxLib.WebServer.WebSocket;

namespace Werewolf.Game;

public class GameWebSocketEndpoint : WebSocketEndpoint<GameWebSocketConnection>
{
    public override string? Protocol => null;

    private readonly EventFactory factory = new EventFactory();

    private readonly Werewolf.User.UserFactory userFactory;

    public GameWebSocketEndpoint(Werewolf.User.UserFactory userFactory)
    {
        this.userFactory = userFactory;
        // fill the factory with the types
        factory.Add<Events.FetchRoles>();
        factory.Add<Events.SubmitRoles>();
        factory.Add<Events.SetGameConfig>();
        factory.Add<Events.SetUserConfig>();
        factory.Add<Events.GameStart>();
        factory.Add<Events.GameNext>();
        factory.Add<Events.GameStop>();
        factory.Add<Events.VotingStart>();
        factory.Add<Events.Vote>();
        factory.Add<Events.VotingWait>();
        factory.Add<Events.VotingFinish>();
        factory.Add<Events.KickUser>();
        factory.Add<Events.Message>();
        factory.Add<Events.RefetchJoinToken>();
    }

    protected override GameWebSocketConnection? CreateConnection(Stream stream, HttpRequestHeader header)
    {
        if (Program.MaintenanceMode)
            return null;
        if (header.Location.DocumentPathTiles.Length != 2)
            return null;
        if (header.Location.DocumentPathTiles[0].ToLowerInvariant() != "ws")
            return null;
        var result = GameController.Current.GetFromToken(
            header.Location.DocumentPathTiles[1]
        );
        return result == null
            ? null
            : new GameWebSocketConnection(stream, factory, userFactory,
                result.Value.game, result.Value.entry
            );
    }
}
