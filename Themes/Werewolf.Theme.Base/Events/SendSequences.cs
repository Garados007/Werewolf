using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public sealed class SendSequences : GameEvent
{
    public override bool CanSendTo(GameRoom game, UserInfo user)
    => true;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        writer.WriteStartArray("sequences");
        foreach (var sequence in game.Sequences)
        {
            writer.WriteStartObject();
            writer.WriteString("name", sequence.Name);
            writer.WriteString("step-name", sequence.StepName);
            writer.WriteNumber("step-index", sequence.Step);
            writer.WriteNumber("step-max", sequence.MaxStep);
            sequence.WriteMeta(writer, game, user);
            writer.WriteEndObject(); // {}
        }
        writer.WriteEndArray(); // sequences
        writer.WriteBoolean("auto-skip", game.AutoFinishRounds && game.Votings.Count == 0);
    }
}
