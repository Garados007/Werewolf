using System.Text.Json;
using Werewolf.Theme;
using System;
using Werewolf.Users.Api;

namespace Werewolf.Game.Events
{
    public class SendGameData : TaggedEvent
    {
        public GameRoom GameRoom { get; }

        public UserInfo User { get; }

        public UserFactory UserFactory { get; }

        public SendGameData(GameRoom gameRoom, UserInfo user, UserFactory userFactory)
        {
            GameRoom = gameRoom;
            User = user;
            UserFactory = userFactory;
        }

        protected override void Read(JsonElement json)
        {
            throw new NotSupportedException();
        }

        private void WriteGameInfo(Utf8JsonWriter writer)
        {
            var ownRole = GameRoom.TryGetRole(User.Id);
            var winner = GameRoom.Winner;
            writer.WriteStartObject("game");

            writer.WriteString("leader", GameRoom.Leader.ToString());

            WritePhaseInfo(writer, ownRole);
            WriteParticipants(writer, ownRole, winner);
            WriteUser(writer);
            WriteWinner(writer, winner);
            WriteRoleConfig(writer);

            writer.WriteBoolean("leader-is-player", GameRoom.LeaderIsPlayer);
            writer.WriteBoolean("dead-can-see-all-roles", GameRoom.DeadCanSeeAllRoles);
            writer.WriteBoolean("all-can-see-role-of-dead", GameRoom.AllCanSeeRoleOfDead);
            writer.WriteBoolean("autostart-votings", GameRoom.AutostartVotings);
            writer.WriteBoolean("autofinish-votings", GameRoom.AutoFinishVotings);
            writer.WriteBoolean("voting-timeout", GameRoom.UseVotingTimeouts);
            writer.WriteBoolean("autofinish-rounds", GameRoom.AutoFinishRounds);

            writer.WriteStartArray("theme");
            writer.WriteStringValue(GameRoom.Theme?.GetType().FullName ?? 
                typeof(Werewolf.Theme.Default.DefaultTheme).FullName);
            writer.WriteStringValue(GameRoom.Theme?.LanguageTheme ?? "default");
            writer.WriteEndArray();

            writer.WriteEndObject();

        }

        private void WritePhaseInfo(Utf8JsonWriter writer, Role? ownRole)
        {
            if (GameRoom.Phase == null)
                writer.WriteNull("phase");
            else
            {
                writer.WriteStartObject("phase"); // phase
                writer.WriteString("lang-id", GameRoom.Phase.Current.LanguageId);

                writer.WriteStartObject("stage");
                writer.WriteString("lang-id", GameRoom.Phase.Stage.LanguageId);
                writer.WriteString("background-id", GameRoom.Phase.Stage.BackgroundId);
                writer.WriteString("theme", GameRoom.Phase.Stage.ColorTheme);
                writer.WriteEndObject();

                writer.WriteStartArray("voting"); // voting
                foreach (var voting in GameRoom.Phase.Current.Votings)
                {
                    if (!Voting.CanViewVoting(GameRoom, User, ownRole, voting))
                        continue;
                    voting.WriteToJson(writer, GameRoom, User);
                }
                writer.WriteEndArray(); //voting
                writer.WriteEndObject(); // phase
            }
        }

        private void WriteParticipants(Utf8JsonWriter writer, Role? ownRole,
            (uint round, ReadOnlyMemory<UserId> winner)? winner
        )
        {
            writer.WriteStartObject("participants");
            foreach (var participant in GameRoom.Participants.ToArray())
            {
                if (participant.Value == null)
                    writer.WriteNull(participant.Key.ToString());
                else
                {
                    var seenRole = Role.GetSeenRole(GameRoom, winner?.round, User, 
                        participant.Key, participant.Value);

                    writer.WriteStartObject(participant.Key.ToString());
                    writer.WriteStartArray("tags");
                    foreach (var tag in Role.GetSeenTags(GameRoom, User, ownRole, participant.Value))
                        writer.WriteStringValue(tag);
                    writer.WriteEndArray();
                    writer.WriteString("role", seenRole?.GetType().Name);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndObject();
        }

        private void WriteUser(Utf8JsonWriter writer)
        {
            writer.WriteStartObject("User");
            foreach (var (id, entry) in GameRoom.UserCache.ToArray())
            {
                writer.WriteStartObject($"{id}");
                writer.WriteString("name", entry.Config.Username);
                writer.WriteString("img", entry.Config.Image);
                writer.WriteStartObject("stats");
                writer.WriteNumber("win-GameRooms", entry.Stats.WinGames);
                writer.WriteNumber("killed", entry.Stats.Killed);
                writer.WriteNumber("loose-GameRooms", entry.Stats.LooseGames);
                writer.WriteNumber("leader", entry.Stats.Leader);
                writer.WriteNumber("level", entry.Stats.Level);
                writer.WriteNumber("current-xp", entry.Stats.CurrentXp);
                writer.WriteNumber("max-xp", entry.Stats.LevelMaxXP);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        private void WriteWinner(Utf8JsonWriter writer, 
            (uint round, ReadOnlyMemory<UserId> winner)? winner
        )
        {
            if (winner != null)
            {
                writer.WriteStartArray("winner");
                foreach (var item in winner.Value.winner.ToArray())
                    writer.WriteStringValue(item.ToString());
                writer.WriteEndArray();
            }
            else writer.WriteNull("winner");
        }

        private void WriteRoleConfig(Utf8JsonWriter writer)
        {
            writer.WriteStartObject("config");
            foreach (var (role, amount) in GameRoom.RoleConfiguration.ToArray())
            {
                writer.WriteNumber(role.GetType().Name, amount);
            }
            writer.WriteEndObject();
        }

        private void WriteUserConfig(Utf8JsonWriter writer)
        {
            var userConfig = UserFactory.GetCachedUser(User.Id) ?? User;
            writer.WriteStartObject("user-config");
            writer.WriteString("theme", 
                string.IsNullOrEmpty(userConfig.Config.ThemeColor) ? "#ffffff"
                    : userConfig.Config.ThemeColor);
            writer.WriteString("background", 
                string.IsNullOrEmpty(userConfig.Config.BackgroundImage) ? ""
                    : userConfig.Config.BackgroundImage);
            writer.WriteString("language", 
                string.IsNullOrEmpty(userConfig.Config.Language) ? "de" 
                    : userConfig.Config.Language);
            writer.WriteEndObject();
        }

        protected override void Write(Utf8JsonWriter writer)
        {
            WriteGameInfo(writer);

            writer.WriteString("user", User.Id.ToString());

            WriteUserConfig(writer);
        }
    }
}