﻿using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class RemoveVoting : GameEvent
    {
        public ulong Id { get; }

        public RemoveVoting(ulong id)
            => Id = id;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("id", Id.ToString());
        }
    }
}
