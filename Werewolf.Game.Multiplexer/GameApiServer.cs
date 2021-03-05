using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werewolf.Game.Api;

namespace Werewolf.Game.Multiplexer
{
    public class GameApiServer : GameApiServerBase
    {
        private ClientConnector? connector;

        public void SetConnector(ClientConnector connector)
            => this.connector = connector;

        public override async Task<GameRoom?> CreateGroup(UserId request, CancellationToken cancellationToken)
        {
            if (connector == null)
                return null;
            // searches for the lowest filled server
            var apis = connector.ServerStates
                .Where(x => x.Value.ActiveRooms < x.Value.MaxRooms && x.Value.MaxRooms > 0)
                .OrderBy(x => (double)x.Value.ActiveRooms / x.Value.MaxRooms)
                .Select(x => x.Key)
                .Select(x => connector.ApiClients
                    .Where(y => y.endPoint == x)
                    .Select(y => y.api)
                    .FirstOrDefault()
                )
                .Where(x => x is not null)
                .Cast<GameApiClient>();
            // contact the server in ascending order to create a group
            foreach (var api in apis)
            {
                var room = await api.CreateGroup(request, cancellationToken);
                if (room != null)
                    return room;
                if (cancellationToken.IsCancellationRequested)
                    return null;
            }
            // no server could create a room
            return null;
        }

        public override async Task<UserId?> GetOrCreateUser(UserCreateInfo request, CancellationToken cancellationToken)
        {
            if (connector == null)
                return null;
            // any of the connected server should fullfill this task.
            // we just ask the one with the lowest connections.
            var apis = connector.ServerStates
                .Where(x => x.Value.ConnectedServer > 0)
                .OrderBy(x => (double)x.Value.ConnectedUser / x.Value.ConnectedServer)
                .Select(x => x.Key)
                .Select(x => connector.ApiClients
                    .Where(y => y.endPoint == x)
                    .Select(y => y.api)
                    .FirstOrDefault()
                )
                .Where(x => x is not null)
                .Cast<GameApiClient>();
            // contact the server in ascending order
            foreach (var api in apis)
            {
                var user = await api.GetOrCreateUser(request, cancellationToken);
                if (user != null)
                    return user;
                if (cancellationToken.IsCancellationRequested)
                    return null;
            }
            // no server could create a user
            return null;
        }

        public override async Task<ServerState?> GetServerState(CancellationToken cancellationToken)
        {
            if (connector == null)
                return null;
            await Task.CompletedTask;
            return connector.MultiplexServerState;
        }

        public override async Task<ActionState?> JoinGroup(GroupUserId request, CancellationToken cancellationToken)
        {
            if (connector == null)
                return null;
            // search for the right server
            var api = connector.ServerStates
                .Where(x => x.Value.ConnectedServerNames.Contains(request.ServerName))
                .Select(x => x.Key)
                .Select(x => connector.ApiClients
                    .Where(y => y.endPoint == x)
                    .Select(y => y.api)
                    .FirstOrDefault()
                )
                .Where(x => x != null)
                .FirstOrDefault();
            if (api == null)
            {
                return new ActionState
                {
                    Success = false,
                    Error = "Server not connected"
                };
            }
            // contact the server for this request
            return await api.JoinGroup(request, cancellationToken);
        }

        public override async Task<ActionState?> LeaveGroup(GroupUserId request, CancellationToken cancellationToken)
        {
            if (connector == null)
                return null;
            // search for the right server
            var api = connector.ServerStates
                .Where(x => x.Value.ConnectedServerNames.Contains(request.ServerName))
                .Select(x => x.Key)
                .Select(x => connector.ApiClients
                    .Where(y => y.endPoint == x)
                    .Select(y => y.api)
                    .FirstOrDefault()
                )
                .Where(x => x != null)
                .FirstOrDefault();
            if (api == null)
            {
                return new ActionState
                {
                    Success = false,
                    Error = "Server not connected"
                };
            }
            // contact the server for this request
            return await api.LeaveGroup(request, cancellationToken);
        }
    }
}
