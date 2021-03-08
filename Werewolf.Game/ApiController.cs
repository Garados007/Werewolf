using sRPC.TCP;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Werewolf.Game.Api;

namespace Werewolf.Game
{
    public class ApiController : IDisposable
    {
        public class GameApiServer : GameApiServerBase
        {
            public ApiController? Api { get; internal set; }

            internal static UserId Convert(Users.Api.UserId value)
                => new UserId { Id = value.Id };

            internal static Users.Api.UserId Convert(UserId value)
                => new Users.Api.UserId { Id = value.Id };

            internal static GameUserInfo Convert(ApiController api, Theme.GameRoom game, Users.Api.UserInfo value)
                => new GameUserInfo
                {
                    Config = new UserConfig
                    {
                        Image = value.Config.Image,
                        Language = value.Config.Language,
                        Username = value.Config.Username,
                    },
                    ConnectedId = new UserConnectedIds
                    {
                        DiscordId = value.ConnectedId.DiscordId,
                        HasDiscordId = value.ConnectedId.HasDiscordId,
                    },
                    Id = Convert(value.Id),
                    JoinUrl = $"{api.domain}game/{GameController.GetUserToken(game, value)}"
                };

            private static Users.Api.UserConfig Convert(UserConfig value)
                => new Users.Api.UserConfig
                {
                    Image = value.Image,
                    Language = value.Language,
                    Username = value.Username,
                };

            private static Users.Api.UserConnectedIds Convert(UserConnectedIds value)
                => new Users.Api.UserConnectedIds
                {
                    DiscordId = value.DiscordId,
                    HasDiscordId = value.HasDiscordId,
                };

            private GameRoom Convert(Theme.GameRoom value)
                => new GameRoom
                {
                    Leader = Convert(value.Leader),
                    MaxUserCount = 500,
                    RoomId = unchecked((uint)value.Id),
                    ServerName = Api?.servername ?? "",
                    User =
                    {
                        value.Users.Keys.Select(x =>
                        {
                            var user = Api?.userController.GetCachedUser(x);
                            return user == null || Api is null
                                ? null
                                : Convert(Api, value, user); })
                        .Where(x => x != null)
                    },
                    UserCount = (uint)value.Users.Count,
                };

            public override async Task<GameRoom?> CreateGroup(UserId request, CancellationToken cancellationToken)
            {
                if (Api == null)
                    return null;
                var user = await Api.userController.GetUser(Convert(request));
                if (user == null)
                    return null;

                if (cancellationToken.IsCancellationRequested)
                    return null;

                var roomId = GameController.Current.CreateGame(user);
                var room = GameController.Current.GetGame(roomId);

                _ = Api.UpdateState(x =>
                {
                    x.ActiveRooms++;
                    return x;
                });

                return room == null ? null : Convert(room);
            }

            public override async Task<UserId?> GetOrCreateUser(UserCreateInfo request, CancellationToken cancellationToken)
            {
                if (Api is null)
                    return null;
                var config = Convert(request.Config);
                var user = await Api.userController.GetOrCreateAsync(
                    Convert(request.ConnectedId),
                    config
                );
                if (user is null || cancellationToken.IsCancellationRequested)
                    return null;
                await Api.userController.UpdateUserConfig(user.Id, config);
                return Convert(user.Id);
            }

            public override Task<ServerState?> GetServerState(CancellationToken cancellationToken)
            {
                return Api is null
                    ? Task.FromResult<ServerState?>(null)
                    : Task.FromResult<ServerState?>(Api.GetState());
            }

            public override async Task<ActionState?> JoinGroup(GroupUserId request, CancellationToken cancellationToken)
            {
                if (Api is null)
                    return null;
                if (request.ServerName != Api.servername)
                    return new ActionState
                    {
                        Success = false,
                        Error = "Invalid server name",
                    };
                var room = GameController.Current.GetGame(unchecked((int)request.RoomId));
                if (room is null)
                    return new ActionState
                    {
                        Success = false,
                        Error = "Room not found",
                    };
                var userId = Convert(request.UserId);
                var user = await Api.userController.GetUser(userId);
                if (user is null)
                    return new ActionState
                    {
                        Success = false,
                        Error = "User not found",
                    };
                var success = room.AddParticipant(user);
                if (!success)
                    return new ActionState
                    {
                        Success = false,
                        Error = "User already added",
                    };

                _ = Api.UpdateState(x =>
                {
                    x.ConnectedUser++;
                    return x;
                });
                _ = Api.NotifyRoomUpdated(room);

                return new ActionState
                {
                    Success = true,
                };
            }

            public override async Task<ActionState?> LeaveGroup(GroupUserId request, CancellationToken cancellationToken)
            {
                if (Api is null)
                    return null;
                if (request.ServerName != Api.servername)
                    return new ActionState
                    {
                        Success = false,
                        Error = "Invalid server name",
                    };
                var room = GameController.Current.GetGame(unchecked((int)request.RoomId));
                if (room is null)
                    return new ActionState
                    {
                        Success = false,
                        Error = "Room not found",
                    };
                var userId = Convert(request.UserId);
                var user = await Api.userController.GetUser(userId);
                if (user is null)
                    return new ActionState
                    {
                        Success = false,
                        Error = "User not found",
                    };
                var success = room.RemoveParticipant(user);
                if (!success)
                    return new ActionState
                    {
                        Success = false,
                        Error = "Cannot remove user",
                    };

                bool gameRemoved = false;
                if (room.Users.IsEmpty && room.Leader == userId)
                {
                    gameRemoved = GameController.Current.RemoveGame(room.Id);
                }

                _ = Api.UpdateState(x =>
                {
                    x.ConnectedUser = Math.Max(0, x.ConnectedUser - 1);
                    if (gameRemoved)
                        x.ActiveRooms = Math.Max(0, x.ActiveRooms - 1);
                    return x;
                });
                _ = Api.NotifyRoomUpdated(room);

                return new ActionState
                {
                    Success = true,
                };
            }
        }

        private readonly TcpApiServer<GameNotificationClient, GameApiServer> api;
        private readonly int timeout;
        private readonly UserController userController;
        private readonly string servername;
        private readonly string domain;
        private bool disposedValue;

        private ServerState state;
        private readonly SemaphoreSlim lockState = new SemaphoreSlim(1, 1);

        public ServerState GetState()
            => state;

        public async Task UpdateState(Func<ServerState, ServerState> updater)
        {
            await lockState.WaitAsync().ConfigureAwait(false);
            var newState = state = updater(state);
            _ = lockState.Release();
            using var timeouter = new CancellationTokenSource(timeout);
            await Task.WhenAll(api.RequestApis.Select(async x =>
            {
                try { await x.ServerUpdated(newState, timeouter.Token); }
                catch (TaskCanceledException)
                {
                    Serilog.Log.Warning("[api] a single endpoint took longer than {timeout} for its update state", timeout);
                }
            }));
        }

        public async Task NotifyRoomUpdated(Theme.GameRoom room)
        {
            var request = new GameRoom
            {
                Leader = GameApiServer.Convert(room.Leader),
                MaxUserCount = 500,
                RoomId = unchecked((uint)room.Id),
                ServerName = servername,
                UserCount = (uint)room.Users.Count,
                User =
                {
                    room.Users.Values.Select(x => GameApiServer.Convert(this, room, x.User))
                }
            };
            using var timeouter = new CancellationTokenSource(timeout);
            await Task.WhenAll(api.RequestApis.Select(async x =>
            {
                try { await x.RoomUpdated(request, timeouter.Token); }
                catch (TaskCanceledException)
                {
                    Serilog.Log.Warning("[api] a single endpoint took longer than {timeout} for its update state", timeout);
                }
            }));
        }

        public ApiController(UserController userController, string servername, string domain, int gameApiPort, int timeout)
        {
            this.userController = userController;
            this.servername = servername;
            this.domain = domain;
            api = new TcpApiServer<GameNotificationClient, GameApiServer>(
                new IPEndPoint(IPAddress.Any, gameApiPort),
                x => { },
                x => x.Api = this
            );
            state = new ServerState
            {
                ActiveRooms = 0,
                ConnectedServer = 1,
                ConnectedServerNames = { servername },
                ConnectedUser = 0,
                MaxRooms = ulong.MaxValue,
            };
            this.timeout = timeout;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    api.Dispose();
                }
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
