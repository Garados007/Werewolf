using System.Threading;
using System.Threading.Tasks;
using Werewolf.Users.Api;
using Google.Protobuf;
using LiteDB;

namespace Werewolf.Users
{
    public class ApiServer : Api.UserApiServerBase
    {
        private static readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

        public Database? Database { get; private set; }

        public void SetDatabase(Database database)
            => Database = database;
        
        private UserId ToApi(ObjectId id)
        {
            return new UserId
            {
                Id = ByteString.CopyFrom(id.ToByteArray()),
            };
        }

        private ObjectId ToDb(UserId id)
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
                    dbUser.Id = new LiteDB.ObjectId();
                    dbUser.Stats = new DbUserStats();
                    @lock.EnterWriteLock();
                    try { Database.User.Insert(dbUser); }
                    finally { @lock.ExitWriteLock(); }
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
            throw new System.NotImplementedException();
        }

        public override Task UpdateUser(UserInfo request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}