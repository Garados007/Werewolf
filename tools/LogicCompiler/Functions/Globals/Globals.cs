using LogicCompiler.Ast;

namespace LogicCompiler.Functions.Globals;

internal sealed class EnabledCharacter : IGlobal
{
    public string Name => "@character";

    public Ast.Type GetPreType(Id name, Context context)
    {
        return Ast.ValueType.Character | Ast.ValueType.Collection;
    }

    public void Write(Output output)
    {
        output.Write("game.EnabledCharacters");
    }

    public void WriteGlobalDoc(Output output)
    {
        Registry.WriteGlobalDoc(
            output,
            Name,
            Ast.ValueType.Character | Ast.ValueType.Collection,
            """
            The full collection of all character that are enabled at the current moment.
            """
        );
    }
}

internal sealed class AllCharacter : IGlobal
{
    public string Name => "@all_character";

    public Ast.Type GetPreType(Id name, Context context)
    {
        return Ast.ValueType.Character | Ast.ValueType.Collection;
    }

    public void Write(Output output)
    {
        output.Write("game.AllCharacters");
    }

    public void WriteGlobalDoc(Output output)
    {
        Registry.WriteGlobalDoc(
            output,
            Name,
            Ast.ValueType.Character | Ast.ValueType.Collection,
            """
            The full collection of all characters.
            """
        );
    }
}

internal sealed class Voting : IGlobal
{
    public string Name => "@voting";

    public Ast.Type GetPreType(Id name, Context context)
    {
        return Ast.ValueType.Voting | Ast.ValueType.Collection;
    }

    public void Write(Output output)
    {
        output.Write("game.Votings");
    }

    public void WriteGlobalDoc(Output output)
    {
        Registry.WriteGlobalDoc(
            output,
            Name,
            Ast.ValueType.Voting | Ast.ValueType.Collection,
            """
            The full collection of all currently existing votings.
            """
        );
    }
}

internal sealed class Event : IGlobal
{
    public string Name => "@event";

    public Ast.Type GetPreType(Id name, Context context)
    {
        return Ast.ValueType.Event | Ast.ValueType.Collection;
    }

    public void Write(Output output)
    {
        output.Write("game.Events");
    }

    public void WriteGlobalDoc(Output output)
    {
        Registry.WriteGlobalDoc(
            output,
            Name,
            Ast.ValueType.Event | Ast.ValueType.Collection,
            """
            The full collection of all currently existing events.
            """
        );
    }
}

internal sealed class Sequence : IGlobal
{
    public string Name => "@sequence";

    public Ast.Type GetPreType(Id name, Context context)
    {
        return Ast.ValueType.Sequence | Ast.ValueType.Collection;
    }

    public void Write(Output output)
    {
        output.Write("game.Sequences");
    }

    public void WriteGlobalDoc(Output output)
    {
        Registry.WriteGlobalDoc(
            output,
            Name,
            Ast.ValueType.Sequence | Ast.ValueType.Collection,
            """
            The full collection of all currently existing sequences.
            """
        );
    }
}
