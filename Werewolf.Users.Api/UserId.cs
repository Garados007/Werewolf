namespace Werewolf.Users.Api
{
    public partial class UserId
    {
        public static bool operator ==(UserId left, UserId right)
            => Equals(left, right);

        public static bool operator !=(UserId left, UserId right)
            => !Equals(left, right);

        public string ToId()
        {
            return Id.ToBase64();
        }

        public void FromId(string id)
        {
            Id = Google.Protobuf.ByteString.FromBase64(id);
        }

        public UserId(string id)
            => FromId(id);
    }
}