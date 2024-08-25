using Antlr4.Runtime;
using LogicCompiler.Grammar;

namespace LogicCompiler.Ast;

internal interface IStatement : ISourceNode
{
    /// <summary>
    /// The type that is parsed and computed bottom up
    /// </summary>
    Type PreType { get; set; }

    /// <summary>
    /// The type that is expected from its parent and provided top down
    /// </summary>
    Type PostType { get; set; }

    /// <summary>
    /// Only used for serialization as debugging aid
    /// </summary>
    Dictionary<string, Type> ContextVariables { get; }

    Type GetPreType(Context context);

    void SetPostType(Context context, Type type);

    void Write(Output output);
}

internal abstract class Statement<T> : AstNode<T>, IStatement
    where T : Antlr4.Runtime.ParserRuleContext
{
    /// <summary>
    /// The type that is parsed and computed bottom up
    /// </summary>
    public Type PreType { get; set; }

    /// <summary>
    /// The type that is expected from its parent and provided top down
    /// </summary>
    public Type PostType { get; set; }

    /// <summary>
    /// Only used for serialization as debugging aid
    /// </summary>
    public Dictionary<string, Type> ContextVariables { get; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public override Antlr4.Runtime.IToken? SourceToken => Source?.Start;

    public Type GetPreType(Context context)
    {
        return PreType = CalcPreType(context);
    }

    public void SetPostType(Context context, Type type)
    {
        context.Flatten(ContextVariables);
        PostType = type;
        TypeHelper.Check(this, PreType, type);
        CalcPostType(context, type);
    }

    public void Write(Output output)
    {
        uint rewrap = PreType.CollectionDepth < PostType.CollectionDepth ? PostType.CollectionDepth - PreType.CollectionDepth : 0u;
        bool mapCharacterInOption = PostType.HasFlag(ValueType.OptionType) && !PreType.HasFlag(ValueType.OptionType)
            && PreType.HasFlag(ValueType.Character);

        for (uint i = 0; i < rewrap; i++)
            output.Write("Werewolf.Theme.Tools.Rewrap(");
        if (mapCharacterInOption && PreType.CollectionDepth == 0)
            output.Write("new CharacterOption(game, ");
        DoWrite(output);
        if (mapCharacterInOption)
        {
            if (PreType.CollectionDepth > 0)
            {
                output.Push();
                output.Write(".Select(_x => (Werewolf.Theme.VoteOption)new CharacterOption(game, _x)");
                output.Pop();
            }
            output.Write(")");
        }
        for (uint i = 0; i < rewrap; i++)
            output.Write(")");
    }

    protected abstract Type CalcPreType(Context context);

    protected abstract void CalcPostType(Context context, Type type);


    protected abstract void DoWrite(Output output);
}

internal interface IExpression : IStatement
{ }

internal abstract class Expression<T> : Statement<T>, IExpression
    where T : Antlr4.Runtime.ParserRuleContext
{ }

internal abstract class SpawnStatement : Statement<W5LogicParser.StmtSpawnContext>
{

}

internal sealed class VotingSpawnStatement : SpawnStatement
{
    private bool outputEach;

    public Id Name { get; set; } = new();

    public Dictionary<string, IExpression> With { get; } = [];

    protected override void DoWrite(Output output)
    {
        if (outputEach)
        {
            if (With.TryGetValue("targets", out var tar))
            {
                output.Write($"foreach (var _target in ");
                tar.Write(output);
                output.WriteLine($")");
            }
            else output.WriteLine($"foreach (var _target in Voting_{Name.Text}.GetTargets(game))");
            output.WriteBlockBegin();
        }
        output.Write($"game.AddVoting(new Voting_{Name.Text}(game");
        if (outputEach)
            output.Write(", _target");
        if (With.TryGetValue("choices", out var exp))
        {
            output.Write(", choices: ");
            exp.Write(output);
            if (!exp.PreType.Flag.HasFlag(ValueType.Option))
            {
                output.Push();
                output.WriteLine(".Select(_x => (Werewolf.Theme.VoteOption)new Werewolf.Theme.CharacterOption(game, _x))");
                output.Pop();
            }
        }
        if (With.TryGetValue("eligable", out exp))
        {
            output.Write(", eligable: ");
            exp.Write(output);
        }
        if (With.TryGetValue("viewer", out exp))
        {
            output.Write(", viewer: ");
            exp.Write(output);
        }
        output.WriteLine("));");
        if (outputEach)
        {
            output.WriteBlockEnd();
        }
    }

    protected override void CalcPostType(Context context, Type type)
    {
        outputEach = context.Generator.Votings.TryGetValue(Name.Text, out var voting)
            && voting.Target is VotingTarget.Each or VotingTarget.MultiEach;
        foreach (var (name, exp) in With)
        {
            switch (name)
            {
                case "choices":
                    exp.SetPostType(context, ValueType.Option | ValueType.Collection);
                    break;
                case "eligable":
                case "viewer":
                    exp.SetPostType(context, ValueType.Character | ValueType.Collection);
                    break;
                case "targets" when outputEach:
                    exp.SetPostType(context, ValueType.Character | ValueType.Collection);
                    break;
                default:
                    if (outputEach)
                        Error.WriteError(exp, $"Unknown with clause `{name}`. Only supported: `targets`, `choices`, `eligable` and `viewer`.");
                    else Error.WriteError(exp, $"Unknown with clause `{name}`. Only supported: `choices`, `eligable` and `viewer`.");
                    break;
            }
        }
    }

    protected override Type CalcPreType(Context context)
    {
        if (!context.Generator.Votings.ContainsKey(Name.Text))
            Error.WriteError(Name, $"Voting {Name.Text} not found");
        foreach (var (_, exp) in With)
            _ = exp.GetPreType(context);
        return ValueType.Void | ValueType.Mutable;
    }
}

internal sealed class SequenceSpawnStatement : SpawnStatement
{
    public Id Name { get; set; } = new();

    public IExpression? Value { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write($"game.AddSequence(new Sequence_{Name.Text}(game, ");
        Value?.Write(output);
        output.WriteLine("));");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Value?.SetPostType(context, ValueType.Character);
    }

    protected override Type CalcPreType(Context context)
    {
        if (!context.Generator.Sequences.ContainsKey(Name.Text))
            Error.WriteError(Name, $"Sequence {Name.Text} not found");
        _ = Value?.GetPreType(context);
        return ValueType.Void | ValueType.Mutable;
    }
}

internal sealed class AnyEventSpawnStatement : SpawnStatement
{
    protected override void DoWrite(Output output)
    {
        output.WriteLine("game.AddRandomEvent();");
    }

    protected override void CalcPostType(Context context, Type type)
    {
    }

    protected override Type CalcPreType(Context context)
    {
        return ValueType.Void | ValueType.Mutable;
    }
}
internal sealed class NotifyPlayerStatement : Statement<W5LogicParser.StmtNotifyPlayerContext>
{
    public Id Name { get; set; } = new();

    public IExpression? Value { get; set; }

    protected override void DoWrite(Output output)
    {
        output.WriteLine($"game.SendEvent(new Werewolf.Theme.Events.PlayerNotification(\"{Name.Text}\",");
        output.Push();
        Value?.Write(output);
        output.WriteLine();
        output.WriteLine(".Select(x => game.TryGetId(x))");
        output.WriteLine(".Where(x => x.HasValue)");
        output.WriteLine(".Select(x => x!.Value)");
        output.WriteLine(".ToArray()");
        output.Pop();
        output.WriteLine("));");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Value?.SetPostType(context, ValueType.Character | ValueType.Collection);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Value?.GetPreType(context);
        return ValueType.Void | ValueType.Mutable;
    }
}


internal sealed class NotifyStatement : Statement<W5LogicParser.StmtNotifyContext>
{
    private VariableExpression? variable;

    public Id? Sequence { get; set; }

    public Id Name { get; set; } = new();

    protected override void DoWrite(Output output)
    {
        output.Write("game.SendEvent(new Werewolf.Theme.Events.CoreNotification(");
        if (Sequence != null)
            output.Write($"\"{Sequence.Text}\"");
        else output.Write("null");
        output.Write(", ");
        if (variable is not null)
            variable.Write(output);
        else if (Name.Text.StartsWith('"'))
            output.Write($"\"{Name.Text[1..^1]}\"");
        else output.Write($"\"{Name.Text}\"");
        output.WriteLine("));");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        variable?.SetPostType(context, ValueType.String);
    }

    protected override Type CalcPreType(Context context)
    {
        if (Sequence is not null && !context.Generator.Sequences.ContainsKey(Sequence.Text))
            Error.WriteError(Sequence, $"Sequence {Sequence.Text} not found");
        if (Name.Text.StartsWith('$'))
        {
            variable = new VariableExpression
            {
                SourceFile = SourceFile,
                Source = new W5LogicParser.ExprVariableContext(new())
                {
                    Start = Name.Source,
                },
                Name = Name,
            };
            _ = variable.GetPreType(context);
        }
        return ValueType.Void | ValueType.Mutable;
    }
}

internal sealed class ConditionalIfStatement : Statement<W5LogicParser.StmtCondIfContext>
{
    public IExpression? Expression { get; set; }

    public List<IStatement> Success { get; } = [];

    public List<IStatement> Fail { get; } = [];

    protected override void DoWrite(Output output)
    {
        output.Write("if (");
        Expression?.Write(output);
        output.WriteLine(")");
        output.WriteBlockBegin();
        foreach (var stmt in Success)
            stmt.Write(output);
        output.WriteBlockEnd();
        if (Fail.Count > 0)
        {
            output.WriteLine("else");
            output.WriteBlockBegin();
            foreach (var stmt in Fail)
                stmt.Write(output);
            output.WriteBlockEnd();
        }
    }

    protected override void CalcPostType(Context context, Type type)
    {
        var mut = type.Flag & ValueType.Mutable;
        Expression?.SetPostType(context, ValueType.Bool);
        foreach (var stmt in Success)
            stmt.SetPostType(context, ValueType.Void | mut);
        foreach (var stmt in Fail)
            stmt.SetPostType(context, ValueType.Void | mut);
    }

    protected override Type CalcPreType(Context context)
    {
        ValueType mut = ValueType.None;
        if (Expression is not null)
            mut |= Expression.GetPreType(context).Flag & ValueType.Mutable;
        foreach (var stmt in Success)
            mut |= stmt.GetPreType(context).Flag & ValueType.Mutable;
        foreach (var stmt in Fail)
            mut |= stmt.GetPreType(context).Flag & ValueType.Mutable;
        return ValueType.Void | mut;
    }
}

internal sealed class IfLetStatement : Expression<W5LogicParser.ExprIfLetContext>
{
    public Id Name { get; set; } = new();

    public IExpression? Value { get; set; }

    public CodeBlock? Success { get; set; }

    public CodeBlock? Fail { get; set; }

    protected override void DoWrite(Output output)
    {
        var hasReturn = !PostType.HasFlag(ValueType.Void);
        if (hasReturn)
        {
            output.Write("(new Func<");
            PostType.Write(output);
            output.Write(">(() =>");
            output.WriteBlockBegin();
        }
        output.Write("if ((");
        Value?.Write(output);
        if (Name.Text == "$_")
            output.WriteLine($").TryPickT0(out _, out _))");
        else output.WriteLine($").TryPickT0(out var @{Name.Text[1..]}, out _))");
        output.WriteBlockBegin();
        Success?.Write(output, hasReturn);
        output.WriteBlockEnd();
        if (hasReturn || Fail != null)
        {
            output.WriteLine("else");
            output.WriteBlockBegin();
            Fail?.Write(output, hasReturn);
            output.WriteBlockEnd();
        }
        if (hasReturn)
        {
            output.WriteBlockEnd();
            output.Write("))()");
        }
    }

    protected override void CalcPostType(Context context, Type type)
    {
        var value = Value?.PreType ?? ValueType.Void;
        Value?.SetPostType(context, value | ValueType.Optional);
        var successContext = new Context(context);
        if (Name.Text != "$_")
            successContext.Add(this);
        Success?.SetPostType(successContext, type);
        Fail?.SetPostType(context, type);
        if (Fail is null && !type.HasFlag(ValueType.Void))
            Error.WriteError(this, "An else branch is expected that returns a value");
    }

    protected override Type CalcPreType(Context context)
    {
        var value = Value?.GetPreType(context) ?? ValueType.Void;
        if (!value.HasFlag(ValueType.Optional))
            Error.WriteError(this, $"The checked value is not an optional type and cannot therefore assigned to this one. Returned type: {value}");
        var successContext = new Context(context);
        if (Name.Text != "$_")
            successContext.Add(this);
        var success = Success?.GetPreType(successContext) ?? ValueType.Void;
        successContext.Check();
        var fail = Fail?.GetPreType(context) ?? ValueType.Void;
        return success | fail;
    }
}

internal sealed class IfStatement : Expression<W5LogicParser.ExprIfContext>
{
    public IExpression? Expression { get; set; }

    public CodeBlock? Success { get; set; }

    public CodeBlock? Fail { get; set; }

    protected override void DoWrite(Output output)
    {
        var hasReturn = !PostType.HasFlag(ValueType.Void);
        if (hasReturn)
        {
            output.Write("(new Func<");
            PostType.Write(output);
            output.Write(">(() =>");
            output.WriteBlockBegin();
        }
        output.Write("if (");
        Expression?.Write(output);
        output.WriteLine(")");
        output.WriteBlockBegin();
        Success?.Write(output, hasReturn);
        output.WriteBlockEnd();
        if (hasReturn || Fail != null)
        {
            output.WriteLine("else");
            output.WriteBlockBegin();
            Fail?.Write(output, hasReturn);
            output.WriteBlockEnd();
        }
        if (hasReturn)
        {
            output.WriteBlockEnd();
            output.Write("))()");
        }
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Expression?.SetPostType(context, ValueType.Bool);
        Success?.SetPostType(context, type);
        Fail?.SetPostType(context, type);
        if (Fail is null && !type.HasFlag(ValueType.Void))
            Error.WriteError(this, "An else branch is expected that returns a value");
        if (type.HasFlag(ValueType.OptionType))
        {
            PreType = new Type(PreType.Flag & ~ValueType.Character, PreType.CollectionDepth);
        }
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Expression?.GetPreType(context);
        var success = Success?.GetPreType(context) ?? ValueType.Void;
        var fail = Fail?.GetPreType(context) ?? ValueType.Void;
        return success | fail;
    }
}

internal sealed class ForLetStatement : Expression<W5LogicParser.ExprForLetContext>
{
    public Id Name { get; set; } = new();

    public IExpression? Value { get; set; }

    public CodeBlock? Loop { get; set; }

    protected override void DoWrite(Output output)
    {
        var hasReturn = !PostType.HasFlag(ValueType.Void);
        if (hasReturn)
        {
            output.Write("(");
            Value?.Write(output);
            output.WriteLine(")");
            output.Push();
            if (Name.Text == "$_")
                output.WriteLine($".Select(_ =>");
            else output.WriteLine($".Select(@{Name.Text[1..]} =>");
        }
        else
        {
            if (Name.Text == "$_")
                output.Write($"foreach (var _ in ");
            else output.Write($"foreach (var @{Name.Text[1..]} in ");
            Value?.Write(output);
            output.WriteLine(")");
        }
        output.WriteBlockBegin();
        Loop?.Write(output, hasReturn);
        if (hasReturn)
        {
            output.Pop();
            output.Write("})");
            output.Pop();
        }
        else
        {
            output.WriteBlockEnd();
        }
    }

    protected override void CalcPostType(Context context, Type type)
    {
        var value = Value?.PreType ?? ValueType.Void;
        Value?.SetPostType(context, value.EnforceCollection());
        var loopContext = new Context(context);
        if (Name.Text != "$_")
            loopContext.Add(this);
        Loop?.SetPostType(loopContext, type.RemoveCollection());
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Value?.GetPreType(context) ?? ValueType.Void;
        var loopContext = new Context(context);
        if (Name.Text != "$_")
            loopContext.Add(this);
        var loop = Loop?.GetPreType(loopContext) ?? ValueType.Void;
        loopContext.Check();
        return loop.AddCollection();
    }
}

internal sealed class LetStatement : Statement<W5LogicParser.StmtLetContext>
{
    public Id Name { get; set; } = new();

    public IExpression? Value { get; set; }

    public bool Redefinition { get; set; }

    protected override void DoWrite(Output output)
    {
        if (!Redefinition)
            output.Write("var ");
        output.Write($"@{Name.Text[1..]} = (");
        Value?.Write(output);
        if (Value?.PostType.HasFlag(ValueType.Collection) ?? false)
            output.Write(").AsList(");
        output.WriteLine(");");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        context.Add(this);
        Value?.SetPostType(context, Value.PreType);
    }

    protected override Type CalcPreType(Context context)
    {
        if (Name.Text is "$game" || Name.Text.StartsWith("$_"))
        {
            Error.WriteError(Name, $"Cannot use reserved variable `{Name.Text}`");
            return ValueType.Void;
        }
        PreType = ValueType.Void | ValueType.Mutable;
        context.Add(this);
        return ValueType.Void | ValueType.Mutable;
    }
}

internal sealed class PipeExpression : Expression<W5LogicParser.ExprPipeContext>
{
    public IExpression? Left { get; set; }

    public List<PipeCall> Right { get; } = [];

    protected override void DoWrite(Output output)
    {
        Left?.Write(output);
        output.Push();
        var consumed = Left?.PostType ?? ValueType.Void;
        foreach (var call in Right)
        {
            output.WriteLine();
            call.Write(output, consumed);
            consumed = call.PostType;
        }
        output.Pop();
        output.WriteLine();
    }

    protected override void CalcPostType(Context context, Type type)
    {
        for (int i = Right.Count - 1; i >= 0; i--)
        {
            var consumed = i > 0 ? Right[i - 1].PreType : Left?.PreType ?? ValueType.Void;
            var next = Right[i].SetPostType(context, consumed, type);
            // undo introduced option conversion. We only want this at the last step of the pipeline
            type = type.HasFlag(ValueType.OptionType)
                && !Right[i].PreType.HasFlag(ValueType.OptionType)
                && Right[i].PreType.HasFlag(ValueType.Character)
                ? new Type(next.Flag & (~ValueType.OptionType), next.CollectionDepth)
                : next;
        }
        Left?.SetPostType(context, type);
    }

    protected override Type CalcPreType(Context context)
    {
        var consumed = Left?.GetPreType(context) ?? ValueType.Void;
        foreach (var pipe in Right)
            consumed = pipe.GetPreType(context, consumed);
        return consumed;
    }
}

internal sealed class PipeCall : AstNode<W5LogicParser.PipeCallContext>
{
    public Id Name { get; set; } = new();

    public List<IExpression> Args { get; } = [];

    public Type PreType { get; set; }
    public Type PostType { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public override Antlr4.Runtime.IToken? SourceToken => Source?.Start;

    public void Write(Output output, Type consumedType)
    {
        if (Functions.Registry.PipedFunctions.TryGetValue(Name.Text, out var func))
        {
            func.Write(output, consumedType, Args, PostType);
        }
    }

    public Type GetPreType(Context context, Type consumedType)
    {
        if (!Functions.Registry.PipedFunctions.TryGetValue(Name.Text, out var func))
        {
            Error.WriteError(Name, $"Function `{Name.Text}` not defined");
            return ValueType.Void;
        }
        if (func is not Functions.ICustomArgumentHandler)
            foreach (var value in Args)
                _ = value.GetPreType(context);
        return PreType = func.GetPreType(Name, context, consumedType, Args);
    }

    public Type SetPostType(Context context, Type consumedType, Type setType)
    {
        TypeHelper.Check(this, PreType, setType);
        PostType = setType;
        return !Functions.Registry.PipedFunctions.TryGetValue(Name.Text, out var func)
            ? (Type)ValueType.Void
            : func.SetPostType(Name, context, consumedType, Args, setType);
    }
}

internal enum CompOp
{
    Equal,
    Unequal,
    GreaterOrEqual,
    LowerOrEqual,
    Greater,
    Lower,
}

internal sealed class CompExpression : Expression<W5LogicParser.ExprCompContext>
{
    public IExpression? Left { get; set; }

    public CompOp Op { get; set; }

    public IExpression? Right { get; set; }

    public bool TypeValueComparison { get; private set; }

    protected override void DoWrite(Output output)
    {
        // perform the special case
        if (TypeValueComparison)
        {
            var (obj, type) = (Left?.PostType.Flag & ValueType.GLOBAL_TYPES) != ValueType.None ?
                (Left, Right) : (Right, Left);
            obj?.Write(output);
            output.Write(" is ");
            type?.Write(output);
            return;
        }

        output.Write("(");
        Left?.Write(output);
        output.Write(" ");
        output.Write(Op switch
        {
            CompOp.Equal => "==",
            CompOp.Unequal => "!=",
            CompOp.GreaterOrEqual => ">=",
            CompOp.LowerOrEqual => "<=",
            CompOp.Greater => ">",
            CompOp.Lower => "<",
            _ => "",
        });
        output.Write(" ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        var left = Left?.PreType ?? ValueType.Void;
        var right = Right?.PreType ?? ValueType.Void;
        // special case: we compare a value with a type, no collection allowed
        if (((left.Flag & ValueType.GLOBAL_TYPES) != ValueType.None) != ((right.Flag & ValueType.GLOBAL_TYPES) != ValueType.None)
            && ((left.Flag & ValueType.GLOBAL_TYPE_INFO) != ValueType.None) != ((right.Flag & ValueType.GLOBAL_TYPE_INFO) != ValueType.None)
            && !left.HasFlag(ValueType.Collection)
            && !right.HasFlag(ValueType.Collection)
        )
        {
            TypeValueComparison = true;
            Left?.SetPostType(context, left.RemoveOptional() | ValueType.ExplicitTypeInfo);
            Right?.SetPostType(context, right.RemoveOptional() | ValueType.ExplicitTypeInfo);
            return;
        }
        TypeValueComparison = false;
        var common = left | right;
        Left?.SetPostType(context, common);
        Right?.SetPostType(context, common);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Bool;
    }
}

internal sealed class OrExpression : Expression<W5LogicParser.ExprOrAndContext>
{
    public IExpression? Left { get; set; }

    public IExpression? Right { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("(");
        Left?.Write(output);
        output.Write(" || ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Left?.SetPostType(context, ValueType.Bool);
        Right?.SetPostType(context, ValueType.Bool);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Bool;
    }
}

internal sealed class AndExpression : Expression<W5LogicParser.ExprOrAndContext>
{
    public IExpression? Left { get; set; }

    public IExpression? Right { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("(");
        Left?.Write(output);
        output.Write(" && ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Left?.SetPostType(context, ValueType.Bool);
        Right?.SetPostType(context, ValueType.Bool);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Bool;
    }
}

internal sealed class AddExpression : Expression<W5LogicParser.ExprAddSubContext>
{
    public IExpression? Left { get; set; }

    public IExpression? Right { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("(");
        Left?.Write(output);
        output.Write(" + ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Left?.SetPostType(context, ValueType.Int);
        Right?.SetPostType(context, ValueType.Int);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Int;
    }
}

internal sealed class SubExpression : Expression<W5LogicParser.ExprAddSubContext>
{
    public IExpression? Left { get; set; }

    public IExpression? Right { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("(");
        Left?.Write(output);
        output.Write(" - ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Left?.SetPostType(context, ValueType.Int);
        Right?.SetPostType(context, ValueType.Int);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Int;
    }
}

internal sealed class MulExpression : Expression<W5LogicParser.ExprMulDivContext>
{
    public IExpression? Left { get; set; }

    public IExpression? Right { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("(");
        Left?.Write(output);
        output.Write(" * ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Left?.SetPostType(context, ValueType.Int);
        Right?.SetPostType(context, ValueType.Int);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Int;
    }
}

internal sealed class DivExpression : Expression<W5LogicParser.ExprMulDivContext>
{
    public IExpression? Left { get; set; }

    public IExpression? Right { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("(");
        Left?.Write(output);
        output.Write(" / ");
        Right?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Left?.SetPostType(context, ValueType.Int);
        Right?.SetPostType(context, ValueType.Int);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Left?.GetPreType(context);
        _ = Right?.GetPreType(context);
        return ValueType.Int;
    }
}

internal sealed class NegateExpression : Expression<W5LogicParser.ExprNegateContext>
{
    public IExpression? Value { get; set; }

    protected override void DoWrite(Output output)
    {
        output.Write("!(");
        Value?.Write(output);
        output.Write(")");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        Value?.SetPostType(context, ValueType.Bool);
    }

    protected override Type CalcPreType(Context context)
    {
        _ = Value?.GetPreType(context);
        return ValueType.Bool;
    }
}

internal sealed class GroupExpression : Expression<W5LogicParser.ExprGroupContext>
{
    public List<IExpression> Values { get; } = [];

    protected override void DoWrite(Output output)
    {
        if (Values.Count == 0)
        {
            output.WriteLine("[]");
            return;
        }
        output.Write("new ");
        PostType.Write(output, true);
        output.WriteLine("()");
        output.WriteBlockBegin();
        foreach (var exp in Values)
        {
            exp.Write(output);
            if (exp.PostType.CollectionDepth > 0)
            {
                output.WriteLine();
                output.Push();
                output.WriteLine(".AsList(),");
                output.Pop();
            }
            else
            {
                output.WriteLine(",");
            }
        }
        output.Pop();
        output.Write("}");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        var subType = type.RemoveCollection();
        foreach (var expr in Values)
            expr.SetPostType(context, subType);
    }

    protected override Type CalcPreType(Context context)
    {
        if (Values.Count == 0)
            return ValueType.Collection;
        Type type = ValueType.None;
        foreach (var expr in Values)
            type |= expr.GetPreType(context);
        return type.AddCollection();
    }
}

internal sealed class GlobalExpression : Expression<W5LogicParser.ExprGlobalContext>
{
    public Id Name { get; set; } = new();

    protected override void DoWrite(Output output)
    {
        if (Functions.Registry.Globals.TryGetValue(Name.Text, out var func))
            func.Write(output);
    }

    protected override void CalcPostType(Context context, Type type)
    {
    }

    protected override Type CalcPreType(Context context)
    {
        if (!Functions.Registry.Globals.TryGetValue(Name.Text, out var func))
        {
            Error.WriteError(Name, $"Global `{Name.Text}` not defined");
            return ValueType.Void;
        }
        return func.GetPreType(Name, context);
    }
}

internal sealed class VariableExpression : Expression<W5LogicParser.ExprVariableContext>
{
    public Id Name { get; set; } = new();

    protected override void DoWrite(Output output)
    {
        output.Write(Name.Text == "$this" ? "this" : $"@{Name.Text[1..]}");
    }

    protected override void CalcPostType(Context context, Type type)
    {
    }

    protected override Type CalcPreType(Context context)
    {
        if (Name.Text.StartsWith("$_"))
        {
            Error.WriteError(Name, $"Cannot use reserved variable `{Name.Text}`");
            return ValueType.Void;
        }
        var info = context.Get(Name.Text);
        if (info is null)
        {
            Error.WriteError(Name, $"Variable {Name.Text} not defined");
            return ValueType.Void;
        }
        info.Use.Add(this);
        return info.Type;
    }
}

internal sealed class IdExpression : Expression<W5LogicParser.ExprCallContext>
{
    public Id Name { get; set; } = new();

    public TypedNameExpression? CalculatedType { get; private set; }

    protected override void DoWrite(Output output)
    {
        if (Name.Text is "true" or "false")
        {
            output.Write(Name.Text);
            return;
        }
        CalculatedType?.Write(output);
    }

    protected override void CalcPostType(Context context, Type type)
    {
        CalculatedType?.SetPostType(context, type);
    }

    protected override Type CalcPreType(Context context)
    {
        CalculatedType = null;
        if (Name.Text is "true" or "false")
            return ValueType.Bool;
        (ValueType, NameType)? Check<T>(ValueType type, NameType name, Dictionary<string, T> dict)
        {
            return dict.ContainsKey(Name.Text) ? (type, name) : null;
        }
        var result = new List<(ValueType, NameType)?>()
        {
            Check(ValueType.ModeType, NameType.Mode, context.Generator.Modes),
            Check(ValueType.CharacterType, NameType.Character, context.Generator.Characters),
            Check(ValueType.WinType, NameType.Win, context.Generator.Wins),
            Check(ValueType.PhaseType, NameType.Phase, context.Generator.Phases),
            Check(ValueType.SceneType, NameType.Scene, context.Generator.Scenes),
            Check(ValueType.VotingType, NameType.Voting, context.Generator.Votings),
            Check(ValueType.SequenceType, NameType.Sequence, context.Generator.Sequences),
            Check(ValueType.EventType, NameType.Event, context.Generator.Events),
            Check(ValueType.OptionType, NameType.Option, context.Generator.Options),
            Check(ValueType.LabelType, NameType.Label, context.Generator.Labels),
        }.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        if (result.Count == 0)
        {
            Error.WriteError(Name, $"Name {Name.Text} not found");
            return ValueType.Void;
        }
        if (result.Count == 1)
        {
            CalculatedType = new TypedNameExpression
            {
                SourceFile = SourceFile,
                Source = new W5LogicParser.ExprTypeContext(new())
                {
                    Start = Source?.Start,
                },
                Name = Name,
                Type = result[0].Item2,
            };
            return CalculatedType.GetPreType(context);
        }
        Error.WriteError(Name, $"The name {Name.Text} is defined multiple times (as {string.Join(", ", result.Select(x => x.Item2.ToString()))}). Annotate the correct type to select which one you want to use.");
        var type = result.Select(x => x.Item1).Aggregate(ValueType.None, (x, y) => x | y);
        return type == ValueType.None ? ValueType.Void : type;
    }
}

internal sealed class CallExpression : Expression<W5LogicParser.ExprCallContext>
{
    public Id Name { get; set; } = new();

    public List<Argument> Values { get; } = [];

    protected override void DoWrite(Output output)
    {
        if (Functions.Registry.CallFunctions.TryGetValue(Name.Text, out var func))
            func.Write(output, Values, PostType);
    }

    protected override void CalcPostType(Context context, Type type)
    {
        if (!Functions.Registry.CallFunctions.TryGetValue(Name.Text, out var func))
        {
            return;
        }
        func.SetPostType(Name, context, Values, type);
    }

    protected override Type CalcPreType(Context context)
    {
        if (!Functions.Registry.CallFunctions.TryGetValue(Name.Text, out var func))
        {
            Error.WriteError(Name, $"Function `{Name.Text}` not defined");
            return ValueType.Void;
        }
        if (func is not Functions.ICustomArgumentHandler)
            foreach (var value in Values)
                _ = value.GetPreType(context);
        var result = func.GetPreType(Name, context, Values);
        return result;
    }
}

internal sealed class Argument : AstNode<W5LogicParser.ArgumentContext>, IStatement
{
    public Id? Name { get; set; }

    public IExpression? Value { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;

    public Type PreType { get; set; }
    public Type PostType { get; set; }

    public Dictionary<string, Type> ContextVariables { get; } = [];

    public Type GetPreType(Context context)
    {
        return PreType = Value?.GetPreType(context) ?? ValueType.None;
    }

    public void SetPostType(Context context, Type type)
    {
        context.Flatten(ContextVariables);
        Value?.SetPostType(context, type);
        PostType = Value?.PostType ?? ValueType.None;
    }

    public void Write(Output output)
    {
        Value?.Write(output);
    }
}

internal sealed class StringExpression : Expression<W5LogicParser.ExprStringContext>
{
    public Id Value { get; set; } = new();

    protected override void DoWrite(Output output)
    {
        output.Write(Value.Text);
    }

    protected override void CalcPostType(Context context, Type type)
    {
    }

    protected override Type CalcPreType(Context context)
    {
        return ValueType.String;
    }
}

internal sealed class IntExpression : Expression<W5LogicParser.ExprIntContext>
{
    public Id Value { get; set; } = new();

    protected override void DoWrite(Output output)
    {
        output.Write(Value.Text);
    }

    protected override void CalcPostType(Context context, Type type)
    {
    }

    protected override Type CalcPreType(Context context)
    {
        return ValueType.Int;
    }
}

internal sealed class TypedNameExpression : Expression<W5LogicParser.ExprTypeContext>
{
    public NameType Type { get; set; }

    public Id Name { get; set; } = new();

    protected override void DoWrite(Output output)
    {
        if ((PostType.Flag & ValueType.GLOBAL_TYPES) != ValueType.None)
            output.Write($"new {Type}_{Name.Text}()");
        else if (PostType.HasFlag(ValueType.ExplicitTypeInfo))
            output.Write($"{Type}_{Name.Text}");
        else output.Write($"typeof({Type}_{Name.Text})");
    }

    protected override void CalcPostType(Context context, Type type)
    {
        if (PreType.HasFlag(ValueType.LabelType))
        {
            var existingMask = PreType.Flag & ValueType.LABEL_TARGET;
            var expectingMask = type.Flag & ValueType.LABEL_TARGET;
            if ((existingMask & expectingMask) != expectingMask)
            {
                Error.WriteError(Name, $"Label `{Name.Text}` cannot be used for {{{expectingMask}}} but can only used for {{{existingMask}}}");
            }
            if (type.HasFlag(ValueType.LabelNoWith) &&
                context.Generator.Labels.TryGetValue(Name.Text, out var label) &&
                label.Withs.Count > 0
            )
            {
                Error.WriteError(Name, $"Label `{Name.Text}` cannot be used at this place, because it has additional information attached to it.");
            }
        }
    }

    private ValueType GetLabelTypeFlag(Context context, ValueType type)
    {
        return !type.HasFlag(ValueType.LabelType) ? ValueType.None :
            context.Generator.Labels[Name.Text].Target.ToType();
    }

    protected override Type CalcPreType(Context context)
    {
        ValueType Check<T>(ValueType type, Dictionary<string, T> dict)
            where T : NodeBase
        {
            if (!dict.TryGetValue(Name.Text, out var value))
            {
                Error.WriteError(Name, $"{Type} {Name.Text} not found");
                return ValueType.Void;
            }
            if (value.IsAbstract)
                Error.WriteError(Name, $"{Type} {Name.Text} is defined as abstract and therefore cannot be referenced.");
            return type;
        }
        var type = Type switch
        {
            NameType.Mode => Check(ValueType.ModeType, context.Generator.Modes),
            NameType.Character => Check(ValueType.CharacterType, context.Generator.Characters),
            NameType.Win => Check(ValueType.WinType, context.Generator.Wins),
            NameType.Phase => Check(ValueType.PhaseType, context.Generator.Phases),
            NameType.Scene => Check(ValueType.SceneType, context.Generator.Scenes),
            NameType.Voting => Check(ValueType.VotingType, context.Generator.Votings),
            NameType.Sequence => Check(ValueType.SequenceType, context.Generator.Sequences),
            NameType.Event => Check(ValueType.EventType, context.Generator.Events),
            NameType.Option => Check(ValueType.OptionType, context.Generator.Options),
            NameType.Label => Check(ValueType.LabelType, context.Generator.Labels),
            _ => throw Error.WriteFatal(Name.SourceFile, Name.Source, $"Invalid type {Type}"),
        };
        return type | GetLabelTypeFlag(context, type);
    }
}

