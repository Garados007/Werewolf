namespace LogicCompiler.Ast;

internal record struct Type(
    ValueType Flag,
    uint CollectionDepth = 0
)
{
    public static implicit operator Type(ValueType type)
    {
        return new Type(type, type.HasFlag(ValueType.Collection) ? 1u : 0);
    }

    public static Type operator |(Type left, Type right)
    {
        return new Type(left.Flag | right.Flag, Math.Max(left.CollectionDepth, right.CollectionDepth));
    }

    public static Type operator &(Type left, Type right)
    {
        return new Type(left.Flag & right.Flag, Math.Min(left.CollectionDepth, right.CollectionDepth));
    }

    public readonly Type EnforceCollection(uint level = 1u)
    {
        return new Type(Flag | ValueType.Collection, Math.Max(level, CollectionDepth));
    }

    public readonly Type AddCollection()
    {
        return new Type(Flag | ValueType.Collection, CollectionDepth + 1);
    }

    public readonly Type RemoveCollection()
    {
        return CollectionDepth <= 1 ? Flag & ~ValueType.Collection :
            new Type(Flag, CollectionDepth - 1);
    }

    public readonly Type MakeOptional()
    {
        return new Type(Flag | ValueType.Optional, CollectionDepth);
    }

    public readonly Type RemoveOptional()
    {
        return new Type(Flag & (~ValueType.Optional), CollectionDepth);
    }

    public readonly bool HasFlag(ValueType flag)
    => Flag.HasFlag(flag);

    public readonly Type Remove(ValueType flag)
    => new(Flag & (~flag), flag.HasFlag(ValueType.Collection) ? 0 : CollectionDepth);

    public readonly void WriteDoc(Output output)
    {
        var raw = Flag & ~(ValueType.Collection | ValueType.Optional | ValueType.Mutable);
        if (Flag.HasFlag(ValueType.Mutable))
            output.Write("**mut** ");
        if (Flag.HasFlag(ValueType.Optional))
            output.Write("Maybe ");
        if (raw != Flag)
            output.Write("{ ");
        if (raw == ValueType.None)
            output.Write("Any");
        else
        {
            bool first = true;
            for (int i = 0; i < 64; i++)
            {
                var flag = raw & (ValueType)(1ul << i);
                if (flag != ValueType.None && Enum.IsDefined(flag))
                {
                    if (first)
                        first = false;
                    else output.Write(", ");
                    output.Write($"{flag}");
                }
            }
        }
        if (raw != Flag)
            output.Write(" }");
        if (CollectionDepth > 0)
            output.Write($" \\[{CollectionDepth}]");
    }
}

[Flags]
public enum ValueType : ulong
{
    // General types

    None = 0x00,
    Bool = 0x01,
    Int = 0x02,
    String = 0x04,

    // special types

    ExplicitTypeInfo = 0x10,
    Mutable = 0x20,
    Collection = 0x40,
    Void = 0x80,

    // Global types

    Mode = 0x0100,
    Phase = 0x0200,
    Scene = 0x0400,
    Label = 0x0800,
    Character = 0x1000,
    Voting = 0x2000,
    Option = 0x4000,
    Win = 0x8000,
    Sequence = 0x1_0000,
    Event = 0x2_0000,

    // Global type info

    ModeType = 0x10_0000,
    PhaseType = 0x20_0000,
    SceneType = 0x40_0000,
    LabelType = 0x80_0000,
    CharacterType = 0x100_0000,
    VotingType = 0x200_0000,
    OptionType = 0x400_0000,
    WinType = 0x800_0000,
    SequenceType = 0x1000_0000,
    EventType = 0x2000_0000,

    // Target

    TargetPhase = 0x1_0000_0000,
    TargetScene = 0x2_0000_0000,
    TargetVoting = 0x4_0000_0000,
    TargetCharacter = 0x8_0000_0000,
    TargetMode = 0x10_0000_0000,

    // special types 2

    Optional = 0x100_0000_0000,
    LabelNoWith = 0x200_0000_0000,

    // Shortcuts

    GENERAL = 0x07,
    SPECIAL = 0x300_0000_00f0,
    GLOBAL_TYPES = 0x3_ff00,
    GLOBAL_TYPE_INFO = 0x3ff0_0000,
    LABEL_TARGET = 0x1f_0000_0000,
}

public enum NameType : byte
{
    Mode,
    Character,
    Win,
    Phase,
    Scene,
    Voting,
    Sequence,
    Event,
    Option,
    Label,
}

internal static class TypeHelper
{
    public static ValueType ToType(this LogicTools.LabelTarget target)
    {
        return target switch
        {
            LogicTools.LabelTarget.Character => ValueType.TargetCharacter,
            LogicTools.LabelTarget.Phase => ValueType.TargetPhase,
            LogicTools.LabelTarget.Scene => ValueType.TargetScene,
            LogicTools.LabelTarget.Voting => ValueType.TargetVoting,
            LogicTools.LabelTarget.Mode => ValueType.TargetMode,
            _ => ValueType.None,
        };
    }

    public static ValueType GetLabelMaskFromValueType(this ValueType type)
    {
        var result = ValueType.None;
        if (type.HasFlag(ValueType.Character))
            result |= ValueType.TargetCharacter;
        if (type.HasFlag(ValueType.Phase))
            result |= ValueType.TargetPhase;
        if (type.HasFlag(ValueType.Scene))
            result |= ValueType.TargetScene;
        if (type.HasFlag(ValueType.Voting))
            result |= ValueType.TargetVoting;
        return result;
    }

    public static void Write(this Type type, Output output, bool concrete = false)
    {
        // output.Write($"/* {type} */ ");

        if (type.HasFlag(ValueType.Optional))
            output.Write("OneOf.OneOf<");

        if (type.CollectionDepth > 0)
            output.Write(concrete ? "List<" : "IEnumerable<");
        for (var i = 1; i < type.CollectionDepth; i++)
            output.Write("List<");

        if (type.HasFlag(ValueType.Void))
            output.Write("void");
        else if (type.HasFlag(ValueType.Character | ValueType.OptionType))
            output.Write("Werewolf.Theme.VoteOption");
        else
        {
            var matches = new List<(ValueType, string)>
            {
                (ValueType.Bool, "bool"),
                (ValueType.Int, "long"),
                (ValueType.String, "string"),
                (ValueType.Mode, "Werewolf.Theme.GameRoom"),
                (ValueType.Phase, "Werewolf.Theme.Phase"),
                (ValueType.Scene, "Werewolf.Theme.Scene"),
                (ValueType.Label, "Werewolf.Theme.Labels.ILabel"),
                (ValueType.Character, "Werewolf.Theme.Character"),
                (ValueType.Voting, "Werewolf.Theme.Voting"),
                (ValueType.Option, "Werewolf.Theme.VoteOption"),
                (ValueType.Win, ""),
                (ValueType.Sequence, "Werewolf.Theme.Sequence"),
                (ValueType.Event, "Werewolf.Theme.Event"),
                (ValueType.GLOBAL_TYPE_INFO, "System.Type"),
            }
                .Where(x => (type.Flag & x.Item1) != ValueType.None)
                .ToList();
            output.Write(matches.Count switch
            {
                0 => "void",
                1 => matches[0].Item2,
                _ => $"OneOf<{string.Join(", ", matches.Select(x => x.Item2))}>",
            });
        }

        for (var i = 0; i < type.CollectionDepth; i++)
            output.Write(">");

        if (type.HasFlag(ValueType.Optional))
            output.Write(", OneOf.Types.None>");
    }

    public static void WriteHostLabelInterfaceType(Output output, ValueType type)
    {
        var labelType = new List<(Ast.ValueType, string)>
        {
            (Ast.ValueType.Mode, "Werewolf.Theme.Labels.IGameRoomLabel"),
            (Ast.ValueType.Character, "Werewolf.Theme.Labels.ICharacterLabel"),
            (Ast.ValueType.Scene, "Werewolf.Theme.Labels.ISceneLabel"),
            (Ast.ValueType.Phase, "Werewolf.Theme.Labels.IPhaseLabel"),
            (Ast.ValueType.Voting, "Werewolf.Theme.Labels.IVotingLabel"),
        }.Where(x => (type & x.Item1) != Ast.ValueType.None)
            .Select(x => x.Item2);
        output.Write(string.Join(", ", labelType));
    }

    public static void CheckIfOnlyOneSet(ISourceNode source, Ast.ValueType type, Ast.ValueType expected)
    {
        bool isCol = expected.HasFlag(ValueType.Collection);
        type &= expected & ~ValueType.Collection;
        if (System.Numerics.BitOperations.PopCount((ulong)type) > 2)
        {
            Error.WriteError(source, $"The {(isCol ? "collection" : "statement")} is expected to return only one type but it does {type}");
        }
    }

    public static void Check(ISourceNode source, Type preType, Type expected)
    {

        // check mutable state
        if (expected.HasFlag(ValueType.Mutable) && !preType.HasFlag(ValueType.Mutable))
        {
            Error.WriteWarning(source, $"This element doesn't modify any value or state and could be safely removed. (returned: {preType}, expected: {expected})");
        }
        if (preType.Flag == ValueType.Collection && expected.HasFlag(ValueType.Collection))
        {
            // used for empty collections
            return;
        }
        if (!expected.HasFlag(ValueType.Void) && preType.HasFlag(ValueType.Void))
        {
            Error.WriteError(source, $"This element created a void value but a non void value was expected. Check the return type of this statement. (returned: {preType}, expected: {expected})");
        }
        // check optional state
        if (!expected.HasFlag(ValueType.Optional) && preType.HasFlag(ValueType.Optional))
        {
            Error.WriteError(source, $"Using an optional at a place where no optional values are allowed. Try to use an if-let construct to extract the value. (returned: {preType}, expected: {expected})");
        }
        // check collection state
        if (expected.CollectionDepth < preType.CollectionDepth && !expected.Flag.HasFlag(ValueType.Void))
        {
            Error.WriteError(
                source,
                $"The returned type has a higher collection depth than expected from its consumer. (returned: {preType}, expected: {expected})"
            );
        }
        // type info collision
        var mask = ValueType.GLOBAL_TYPE_INFO | ValueType.GLOBAL_TYPES;
        if ((expected.Flag & mask) != ValueType.None &&
            (expected.Flag & preType.Flag & mask) == ValueType.None &&
            !(expected.Flag.HasFlag(ValueType.Option) && preType.Flag.HasFlag(ValueType.Character))
        )
        {
            Error.WriteError(source,
                $"The expected value type is not returned. Expected: {expected.Flag & mask}. Returned: {preType.Flag & mask}"
            );
        }
        // conflicting general types
        if (!expected.HasFlag(ValueType.Void) && ((expected.Flag & Ast.ValueType.GENERAL) != (preType.Flag & Ast.ValueType.GENERAL)))
        {
            Error.WriteError(source,
                $"type {preType.Flag} was returned but {expected.Flag} was expected"
            );
        }
    }
}
