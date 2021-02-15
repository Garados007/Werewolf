using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class AddParticipant : GameEvent
    {
        public UserInfo User { get; }

        public AddParticipant(UserInfo user)
            => User = user;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            var entry = User;
            writer.WriteString("id", User.Id.ToString());
            writer.WriteString("name", entry.Config.Username);
            writer.WriteString("img", entry.Config.Image);
            writer.WriteStartObject("stats");
            writer.WriteNumber("win-games", entry.Stats.WinGames);
            writer.WriteNumber("killed", entry.Stats.Killed);
            writer.WriteNumber("loose-games", entry.Stats.LooseGames);
            writer.WriteNumber("leader", entry.Stats.Leader);
            writer.WriteNumber("level", entry.Stats.Level);
            writer.WriteNumber("current-xp", entry.Stats.CurrentXp);
            writer.WriteNumber("max-xp", entry.Stats.LevelMaxXP);
            writer.WriteEndObject();
        }
    }
}