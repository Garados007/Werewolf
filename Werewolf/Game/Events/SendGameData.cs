using System.Text.Json;
using Werewolf.Theme;
using System;
using Werewolf.User;

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
            WriteGameUserEntries(writer, ownRole, winner);
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
            writer.WriteEndArray(); // end of theme

            writer.WriteEndObject(); // game

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

        private void WriteGameUserEntries(Utf8JsonWriter writer, Role? ownRole,
            (uint round, ReadOnlyMemory<UserId> winner)? winner
        )
        {
            writer.WriteStartObject("users");
            foreach (var (id, entry) in GameRoom.Users.ToArray())
            {
                writer.WriteStartObject(id);
                if (entry.Role is null)
                    writer.WriteNull("role");
                else
                {
                    var seenRole = Role.GetSeenRole(GameRoom, winner?.round, User, id, entry.Role);
                    
                    writer.WriteStartObject("role");
                    writer.WriteStartArray("tags");
                    foreach (var tag in Role.GetSeenTags(GameRoom, User, ownRole, entry.Role))
                        writer.WriteStringValue(tag);
                    writer.WriteEndArray(); // tags
                    writer.WriteString("role", seenRole?.GetType().Name);
                    
                    writer.WriteEndObject(); // role
                }

                writer.WriteStartObject("user");
                entry.User.WriteContent(writer);
                writer.WriteEndObject(); // end of user

                writer.WriteBoolean("is-online", entry.IsOnline);
                writer.WriteNumber("online-counter", entry.ConnectionChanged);
                writer.WriteString("last-online-change", entry.LastConnectionUpdate);

                writer.WriteEndObject(); // end of id
            }
            writer.WriteEndObject(); // end of users
        }

        private static void WriteWinner(Utf8JsonWriter writer, 
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
                string.IsNullOrEmpty(userConfig.Config.ThemeColor) ? "#333333"
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