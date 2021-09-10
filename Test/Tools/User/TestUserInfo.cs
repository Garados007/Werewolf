using MongoDB.Bson;
using Werewolf.User;

namespace Test.Tools.User
{
    public class TestUserInfo : UserInfo
    {
        public int Index { get; }

        public TestUserInfo(int index)
        {
            Index = index;
            Id = new UserId(ObjectId.GenerateNewId(Index));
        }

        public override UserId Id { get; }

        public override string? OAuthId => null;

        public override UserConfig Config => new TestUserConfig(Index);

        public override UserStats Stats => new TestUserStats();
    }
}