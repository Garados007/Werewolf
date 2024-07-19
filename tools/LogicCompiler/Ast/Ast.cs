using System.Text.Json;
using Antlr4.Runtime;
using LogicCompiler.Grammar;

namespace LogicCompiler.Ast;

internal abstract class AstNode<T> : ISourceNode
{
    [System.Text.Json.Serialization.JsonIgnore]
    public T? Source { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(FileInfoConverter))]
    public FileInfo? SourceFile { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public abstract IToken? SourceToken { get; }
}

internal sealed class FileInfoConverter : System.Text.Json.Serialization.JsonConverter<FileInfo>
{
    public override FileInfo? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, FileInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FullName);
    }
}

internal interface ISourceNode
{
    [System.Text.Json.Serialization.JsonIgnore]
    IToken? SourceToken { get; }

    [System.Text.Json.Serialization.JsonConverter(typeof(FileInfoConverter))]
    FileInfo? SourceFile { get; }
}

internal interface ICodeContainer
{
    List<Func> Funcs { get; }
}

internal sealed class Id : AstNode<Antlr4.Runtime.IToken>, ISourceNode
{
    public string Text { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source;
}

internal abstract class NodeBase : AstNode<W5LogicParser.NodeBaseContext>, ISourceNode
{
    public bool IsAbstract { get; set; }

    public Id Name { get; set; } = new();

    public List<Id> Inherits { get; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;
}

internal interface IResolvable<T>
    where T : NodeBase
{
    T Resolve(Dictionary<string, T> dict);
}

internal static class Utils
{
    public static void Minify(List<Id> ids)
    {
        var removals = new HashSet<Id>();
        var names = new HashSet<string>();
        foreach (var id in ids)
        {
            if (!names.Add(id.Text))
                _ = removals.Add(id);
        }
        _ = ids.RemoveAll(x => removals.Contains(x));
    }
}

internal sealed class ModeNode : NodeBase, IResolvable<ModeNode>
{
    public List<Id> Character { get; } = [];

    public List<Id> Win { get; } = [];

    public ModeNode Resolve(Dictionary<string, ModeNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new ModeNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
            Character = { Character },
            Win = { Win },
        };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            result.Character.AddRange(other.Character);
            result.Win.AddRange(other.Win);
        }
        Utils.Minify(result.Character);
        Utils.Minify(result.Win);
        return result;
    }
}

internal sealed class PhaseNode : NodeBase, IResolvable<PhaseNode>, ICodeContainer
{
    public List<Func> Funcs { get; } = [];

    public PhaseNode Resolve(Dictionary<string, PhaseNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new PhaseNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
        };
        var rawFuncs = new List<Func> { Funcs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            rawFuncs.AddRange(other.Funcs);
        }
        result.Funcs.AddRange(Func.Combine(rawFuncs));
        return result;
    }
}

internal sealed class SceneNode : NodeBase, IResolvable<SceneNode>, ICodeContainer
{
    public Id? Phase { get; set; }

    public List<Id> Before { get; } = [];

    public List<Id> After { get; } = [];

    public List<Func> Funcs { get; } = [];

    public SceneNode Resolve(Dictionary<string, SceneNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new SceneNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
            Phase = Phase,
            Before = { Before },
            After = { After },
        };
        var rawFuncs = new List<Func> { Funcs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            result.Phase ??= other.Phase;
            result.Before.AddRange(other.Before);
            result.After.AddRange(other.After);
            rawFuncs.AddRange(other.Funcs);
        }
        Utils.Minify(result.Before);
        Utils.Minify(result.After);
        result.Funcs.AddRange(Func.Combine(rawFuncs));
        return result;
    }
}

internal enum LabelTarget
{
    Character = 1,
    Phase = 2,
    Scene = 4,
    Voting = 8,
    Mode = 16,
}

internal sealed class LabelNode : NodeBase, IResolvable<LabelNode>, ICodeContainer
{
    public LabelTarget Target { get; set; }

    public List<Func> Funcs { get; } = [];

    public List<With> Withs { get; } = [];

    public With? GetWith(string name)
    => Withs.FirstOrDefault(x => x.Name.Text == name);

    public LabelNode Resolve(Dictionary<string, LabelNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new LabelNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
            Target = Target,
        };
        var rawFuncs = new List<Func> { Funcs };
        var rawWiths = new List<With> { Withs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            result.Target |= other.Target;
            rawFuncs.AddRange(other.Funcs);
            rawWiths.AddRange(other.Withs);
        }
        result.Funcs.AddRange(Func.Combine(rawFuncs));
        result.Withs.AddRange(With.Combine(rawWiths));
        return result;
    }
}

internal sealed class CharacterNode : NodeBase, IResolvable<CharacterNode>, ICodeContainer
{
    public List<Func> Funcs { get; } = [];

    public CharacterNode Resolve(Dictionary<string, CharacterNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new CharacterNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
        };
        var rawFuncs = new List<Func> { Funcs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            rawFuncs.AddRange(other.Funcs);
        }
        result.Funcs.AddRange(Func.Combine(rawFuncs));
        return result;
    }
}

internal enum VotingTarget
{
    All,
    Each,
    MultiEach,
}

internal sealed class VotingNode : NodeBase, IResolvable<VotingNode>, ICodeContainer
{
    public VotingTarget? Target { get; set; }

    public List<Func> Funcs { get; } = [];

    public VotingNode Resolve(Dictionary<string, VotingNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new VotingNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
            Target = Target,
        };
        var rawFuncs = new List<Func> { Funcs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            result.Target ??= other.Target;
            rawFuncs.AddRange(other.Funcs);
        }
        result.Funcs.AddRange(Func.Combine(rawFuncs));
        return result;
    }
}

internal sealed class OptionNode : NodeBase
{
}

internal sealed class WinNode : NodeBase, IResolvable<WinNode>, ICodeContainer
{
    public List<Func> Funcs { get; } = [];

    public WinNode Resolve(Dictionary<string, WinNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new WinNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
        };
        var rawFuncs = new List<Func> { Funcs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            rawFuncs.AddRange(other.Funcs);
        }
        result.Funcs.AddRange(Func.Combine(rawFuncs));
        return result;
    }
}

internal sealed class SequenceNode : NodeBase, IResolvable<SequenceNode>
{
    public List<Step> Steps { get; } = [];

    public SequenceNode Resolve(Dictionary<string, SequenceNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new SequenceNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
        };
        var rawSteps = new List<Step> { Steps };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            rawSteps.AddRange(other.Steps);
        }
        result.Steps.AddRange(Step.Combine(rawSteps));
        return result;
    }
}

internal sealed class EventNode : NodeBase, IResolvable<EventNode>, ICodeContainer
{
    public List<EventTarget> Targets { get; } = [];

    public List<Func> Funcs { get; } = [];

    public EventNode Resolve(Dictionary<string, EventNode> dict)
    {
        if (Inherits.Count == 0)
            return this;
        var result = new EventNode
        {
            SourceFile = SourceFile,
            IsAbstract = IsAbstract,
            Name = Name,
        };
        var rawTargets = new List<EventTarget> { Targets };
        var rawFuncs = new List<Func> { Funcs };
        foreach (var name in Inherits)
        {
            var other = dict[name.Text].Resolve(dict);
            rawTargets.AddRange(other.Targets);
            rawFuncs.AddRange(other.Funcs);
        }
        result.Targets.AddRange(EventTarget.Combine(rawTargets));
        return result;
    }
}

internal enum TypeSpecifier
{
    Phase,
    Scene,
}

internal sealed class EventTarget : AstNode<W5LogicParser.EventTargetContext>
{
    public Id Target { get; set; } = new();

    public TypeSpecifier? TargetSpecifier { get; set; }

    public List<Step> Steps { get; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;

    public static IEnumerable<EventTarget> Combine(IEnumerable<EventTarget> targets)
    {
        return targets.GroupBy(x => (x.Target.Text, x.TargetSpecifier))
            .Select(x => new EventTarget
            {
                Target = { Text = x.Key.Text },
                TargetSpecifier = x.Key.TargetSpecifier,
                Steps = { x.SelectMany(x => x.Steps) },
            });
    }
}

internal sealed class With : AstNode<W5LogicParser.WithContext>
{
    public Id Type { get; set; } = new();

    public Id Name { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;

    public static IEnumerable<With> Combine(IEnumerable<With> withs)
    {
        var dict = new Dictionary<string, With>();
        foreach (var with in withs)
            if (!dict.TryAdd(with.Name.Text, with))
            {
                var other = dict[with.Name.Text];
                if (with.Type.Text == other.Type.Text)
                    continue;
                Error.WriteError(with, $"Cannot redefine member {with.Name.Text} with type {with.Type.Text} ...");
                Error.WriteWarning(other, $"... when it was defined with type {other.Type.Text} here.");
            }
        return dict.Values;
    }

    public Ast.Type GetValueType()
    {
        return Type.Text switch
        {
            "character" => Ast.ValueType.Character,
            "string" => Ast.ValueType.String,
            "int" => Ast.ValueType.Int,
            "bool" => Ast.ValueType.Bool,
            _ => throw Error.WriteFatal(Type, $"Cannot convert type {Type.Text}"),
        };
    }
}

internal sealed class Func : AstNode<W5LogicParser.FuncContext>
{
    public Id Id { get; set; } = new();

    public CodeBlock Code { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;

    public static IEnumerable<Func> Combine(IEnumerable<Func> trigger)
    {
        return trigger.GroupBy(x => x.Id.Text)
            .Select(x => new Func
            {
                Id = { Text = x.Key },
                Code = CodeBlock.Combine(x.Select(x => x.Code))
            });
    }
}

internal sealed class Step : AstNode<W5LogicParser.StepContext>
{
    public Id Id { get; set; } = new();

    public CodeBlock Code { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;

    public static IEnumerable<Step> Combine(IEnumerable<Step> steps)
    {
        return steps.GroupBy(x => x.Id.Text)
            .Select(x => new Step
            {
                Id = { Text = x.Key },
                Code = CodeBlock.Combine(x.Select(x => x.Code))
            });
    }
}

internal sealed class CodeBlock : AstNode<W5LogicParser.CodeBlockContext>, IStatement
{
    public List<IStatement> Statements { get; } = [];
    public Type PreType { get; set; }
    public Type PostType { get; set; }
    public Dictionary<string, Type> ContextVariables { get; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public override IToken? SourceToken => Source?.Start;

    public static CodeBlock Combine(IEnumerable<CodeBlock> blocks)
    {
        var result = new CodeBlock();
        foreach (var block in blocks.Reverse())
        {
            result.SourceFile ??= block.SourceFile;
            result.Source ??= block.Source;
            result.Statements.AddRange(block.Statements);
        }
        return result;
    }

    public Type GetPreType(Context context)
    {
        context = new(context);
        Type type = ValueType.Void;
        var mut = ValueType.None;
        foreach (var stmt in Statements)
        {
            type = stmt.GetPreType(context);
            mut |= type.Flag & ValueType.Mutable;
        }
        context.Check();
        return PreType = type | mut;
    }

    public void SetPostType(Context context, Type type)
    {
        context.Flatten(ContextVariables);
        context = new(context);
        for (int i = 0; i < Statements.Count - 1; i++)
            Statements[i].SetPostType(context, ValueType.Void | ValueType.Mutable);
        if (Statements.Count > 0)
            Statements[^1].SetPostType(context, type);
        PostType = type;
    }

    public void Write(Output output, bool hasReturn)
    {
        output.WriteLine($"// pre:  {PreType}");
        output.WriteLine($"// post: {PostType}");
        for (int i = 0; i < Statements.Count; i++)
        {
            if (hasReturn && i + 1 == Statements.Count)
                output.Write("return ");
            Statements[i].Write(output);
            if (Statements[i] is IExpression ||
                (Statements[i] is IfLetStatement ifLetStmt && !ifLetStmt.PostType.HasFlag(ValueType.Void)) ||
                (Statements[i] is IfStatement ifStmt && !ifStmt.PostType.HasFlag(ValueType.Void))
            )
                output.WriteLine(";");
        }
    }

    public void Write(Output output)
    {
        Write(output, !PostType.HasFlag(ValueType.Void));
    }
}
