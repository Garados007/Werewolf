﻿using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class GameEnd : GameEvent
    {
        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            if (game.Winner == null)
                writer.WriteNull("winner");
            else
            {
                writer.WriteStartArray("winner");
                foreach (var id in game.Winner.Value.winner.Span)
                    writer.WriteStringValue(id.ToString());
                writer.WriteEndArray();
            }
        }
    }
}
