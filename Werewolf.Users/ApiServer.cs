using System.Threading;
using System.Threading.Tasks;
using Werewolf.Users.Api;
using Google.Protobuf;
using LiteDB;
using sRPC.TCP;
using Serilog;

namespace Werewolf.Users
{
    public class ApiServer : UserApiServerBase
    {
        private static readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

        private TcpApiServer<UserNotificationClient, ApiServer>? api = null;

        public Database? Database { get; private set; }

        public void Set(Database database, TcpApiServer<UserNotificationClient, ApiServer> api)
            => (Database, this.api) = (database, api);
        
        private static UserId ToApi(ObjectId id)
        {
            return new UserId
            {
                Id = ByteString.CopyFrom(id.ToByteArray()),
            };
        }

        private static ObjectId ToDb(UserId id)
        {
            return new ObjectId(id.Id.ToByteArray());
        }

        public override async Task<UserId?> CreateUser(UserInfo request, CancellationToken cancellationToken)
        {
            if (Database == null)
                return null;
            return await Task.Run(() => 
            {
                var dbUser = new DbUser(request);
                @lock.EnterUpgradeableReadLock();
                try 
                {
                    // search for Discord ID
                    var otherUser = Database.User.Query()
                        .Where(x => x.ConnectedIds.DiscordId == dbUser.ConnectedIds.DiscordId)
                        .FirstOrDefault();
                    if (otherUser != null)
                        return ToApi(otherUser.Id);

                    // create new entry
                    dbUser.Id = new ObjectId();
                    dbUser.Stats = new DbUserStats();
                    @lock.EnterWriteLock();
                    try { Database.User.Insert(dbUser); }
                    finally { @lock.ExitWriteLock(); }

                    Log.Verbose("User {id} ({name}) created", dbUser.Id, dbUser.Config.Username);

                    return ToApi(dbUser.Id);
                }
                finally
                {
                    @lock.ExitUpgradeableReadLock();
                }
            });
        }

        public override async Task<UserInfo?> GetUser(UserId request, CancellationToken cancellationToken)
        {
            if (Database == null)
                return null;
            return await Task.Run(() => 
            {
                var id = ToDb(request);
                @lock.EnterReadLock();
                try 
                {
                    var user = Database.User.Query()
                        .Where(x => x.Id == id)
                        .FirstOrDefault();
                    return user?.ToApi();
                }
                finally
                {
                    @lock.ExitReadLock();
                }
            });
        }

        public override Task UpdateStats(UpdateUserStats request, CancellationToken cancellationToken)
        {
            if (Database == null || api == null)
                return Task.CompletedTask;
            return Task.Run(() =>
            {
                var id = ToDb(request.Id);
                UserInfo? info = null;
                @lock.EnterWriteLock();
                try
                {
                    var user = Database.User.Query()
                        .Where(x => x.Id == id)
                        .FirstOrDefault();
                    if (user == null)
                        return;

                    user.Stats.CurrentXp += request.Stats.CurrentXp;
                    user.Stats.Killed += request.Stats.Killed;
                    user.Stats.Leader += request.Stats.Leader;
                    user.Stats.LooseGames += request.Stats.LooseGames;
                    user.Stats.WinGames += request.Stats.WinGames;

                    request.Stats.Level = user.Stats.Level;
                    ulong max;
                    while (user.Stats.CurrentXp >= (max = request.Stats.LevelMaxXP))
                    {
                        request.Stats.Level = ++user.Stats.Level;
                        user.Stats.CurrentXp -= max;
                    }

                    Database.User.Update(user);
                    info = user.ToApi();

                    Log.Verbose("Stats from {id} ({name}) updated", user.Id, user.Config.Username);
                }
                finally
                {
                    @lock.ExitWriteLock();
                }
                if (info != null)
                    foreach (var client in api.RequestApis)
                        _ = client.UpdatedUser(info);
            }, cancellationToken);
        }

        public override Task UpdateUser(UserInfo request, CancellationToken cancellationToken)
        {
            if (Database == null || api == null)
                return Task.CompletedTask;
            return Task.Run(() =>
            {
                var id = ToDb(request.Id);
                UserInfo? info = null;
                @lock.EnterWriteLock();
                try
                {
                    var user = Database.User.Query()
                        .Where(x => x.Id == id)
                        .FirstOrDefault();
                    if (user == null)
                        return;

                    user.Config = new DbUserConfig(request.Config);
                    user.ConnectedIds = new DbUserConnected(request.ConnectedId);

                    Database.User.Update(user);
                    info = user.ToApi();
                    Log.Verbose("User {id} ({name}) updated", user.Id, user.Config.Username);
                }
                finally
                {
                    @lock.ExitWriteLock();
                }
                if (info != null)
                    foreach (var client in api.RequestApis)
                        _ = client.UpdatedUser(info);
            }, cancellationToken);
        }

        public override Task<UserId?> FindUser(UserConnectedIds request, CancellationToken cancellationToken)
        {
            if (Database == null || api == null)
                return Task.FromResult<UserId?>(null);
            return Task.Run(() =>
            {
                @lock.EnterReadLock();
                DbUser? user = null;
                try
                {
                    // check for discord id
                    if (user == null && request.HasDiscordId)
                        user = Database.User.Query()
                            .Where(x => x.ConnectedIds.DiscordId == request.DiscordId)
                            .FirstOrDefault();
                    // no id system found
                }
                finally
                {
                    @lock.ExitReadLock();
                }
                return user == null ? null : ToApi(user.Id);
            });
        }
    }
}