using MongoDB.Bson.Serialization.Attributes;

namespace Werewolf.User.DB
{
    [BsonIgnoreExtraElements]
    public class UserConfig
    {
        [BsonRequired]
        public string Username { get; set; } = "";

        [BsonRequired]
        public string Image { get; set; } = ""; 

        [BsonRequired]
        public string ThemeColor { get; set; } = "#ffffff";

        public string? BackgroundImage { get; set; }

        [BsonRequired]
        public string Language { get; set; } = "en";
    }
}