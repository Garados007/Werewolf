using System;
using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class SetGameConfig : GameEvent
    {
        public Type DefaultTheme { get; }

        public SetGameConfig(Type defaultTheme)
        {
            DefaultTheme = defaultTheme;
        }

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteStartObject("config");
            foreach (var (role, amount) in game.RoleConfiguration.ToArray())
            {
                writer.WriteNumber(role.GetType().Name, amount);
            }
            writer.WriteEndObject();

            writer.WriteBoolean("leader-is-player", game.LeaderIsPlayer);
            writer.WriteBoolean("dead-can-see-all-roles", game.DeadCanSeeAllRoles);
            writer.WriteBoolean("all-can-see-role-of-dead", game.AllCanSeeRoleOfDead);
            writer.WriteBoolean("autostart-votings", game.AutostartVotings);
            writer.WriteBoolean("autofinish-votings", game.AutoFinishVotings);
            writer.WriteBoolean("voting-timeout", game.UseVotingTimeouts);
            writer.WriteBoolean("autofinish-rounds", game.AutoFinishRounds);

            writer.WriteStartArray("theme");
            writer.WriteStringValue(game.Theme?.GetType().FullName ?? DefaultTheme.FullName);
            writer.WriteStringValue(game.Theme?.LanguageTheme ?? "default");
            writer.WriteEndArray();
        }
    }
}
