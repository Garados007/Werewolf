using LogicCompiler.Grammar;

namespace LogicCompiler.Ast;


internal sealed class File
{
    public List<ModeNode> Modes { get; } = [];
    public List<PhaseNode> Phases { get; } = [];
    public List<SceneNode> Scenes { get; } = [];
    public List<LabelNode> Labels { get; } = [];
    public List<CharacterNode> Characters { get; } = [];
    public List<VotingNode> Votings { get; } = [];
    public List<OptionNode> Options { get; } = [];
    public List<WinNode> Wins { get; } = [];
    public List<SequenceNode> Sequences { get; } = [];
    public List<EventNode> Events { get; } = [];

    public FileInfo Source { get; }

    public File(FileInfo source, W5LogicParser.ProgramContext program)
    {
        Source = source;
        foreach (var progNode in program._nodes)
        {
            bool isAbstract = progNode.isAbstract != null;
            switch (progNode.spec)
            {
                case W5LogicParser.ModeNodeContext node:
                    Modes.Add(new ModeNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Character = { node.node._characters.SelectMany(x => Get(x)) },
                        Win = { node.node._wins.SelectMany(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.PhaseNodeContext node:
                    Phases.Add(new PhaseNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.SceneNodeContext node:
                    Scenes.Add(new SceneNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Phase = node.node._phases.Select(x => GetId(x)).OptionalFirstOrError(),
                        Before = { node.node._cycles.Where(x => x.cycle.Type == W5LogicParser.KW_BEFORE)
                            .SelectMany(x => Get(x.ids))
                        },
                        After = { node.node._cycles.Where(x => x.cycle.Type == W5LogicParser.KW_AFTER)
                            .SelectMany(x => Get(x.ids))
                        },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.LabelNodeContext node:
                    Labels.Add(new LabelNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Target = GetLabelTarget(node.node._targets),
                        Withs = { node.node._withs.Select(x => Get(x)) },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.CharacterNodeContext node:
                    Characters.Add(new CharacterNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.VotingNodeContext node:
                    Votings.Add(new VotingNode
                    {
                        Target = GetVotingTarget(node.node._targets),
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.OptionNodeContext node:
                    Options.Add(new OptionNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                    });
                    break;
                case W5LogicParser.WinNodeContext node:
                    Wins.Add(new WinNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.SequenceNodeContext node:
                    Sequences.Add(new SequenceNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Steps = { node.node._steps.Select(x => Get(x)) },
                    });
                    break;
                case W5LogicParser.EventNodeContext node:
                    Events.Add(new EventNode
                    {
                        Source = progNode,
                        SourceFile = source,
                        IsAbstract = isAbstract,
                        Name = GetId(node.node.name),
                        Inherits = { Get(node.node.@base) },
                        Targets = { node.node._targets.Select(x => Get(x)) },
                        Funcs = { node.node._funcs.Select(x => Get(x)) },
                    });
                    break;
                default: throw Error.WriteFatal(Source, progNode.spec.Start, $"Unexpected variant: {progNode.spec}");
            }
        }
    }

    private Id GetId(Antlr4.Runtime.IToken id)
    {
        return new Id
        {
            Source = id,
            SourceFile = Source,
            Text = id.Text,
        };
    }

    private IEnumerable<Id> Get(W5LogicParser.IdListContext ctx)
    {
        foreach (var item in ctx._names)
            yield return GetId(item);
    }

    private IEnumerable<Id> Get(W5LogicParser.InheritContext? ctx)
    {
        if (ctx is null)
            yield break;
        foreach (var item in ctx._ids)
            yield return GetId(item);
    }

    private VotingTarget? GetVotingTarget(IList<Antlr4.Runtime.IToken> tokens)
    {
        if (tokens.Count == 0)
            return null;
        if (tokens.Count > 1)
        {
            foreach (var token in tokens)
                Error.WriteError(Source, token, $"Only up to one target specification was expected but multiple was found.");
            return null;
        }
        switch (tokens[0].Text)
        {
            case "all": return VotingTarget.All;
            case "each": return VotingTarget.Each;
            case "multi_each": return VotingTarget.MultiEach;
            default:
                Error.WriteError(Source, tokens[0], $"Invalid target `{tokens[0].Text}`. Only `all`, `each` and `multi_each` are supported.");
                return null;
        }
    }

    private Func Get(W5LogicParser.FuncContext ctx)
    {
        return new Func
        {
            Source = ctx,
            SourceFile = Source,
            Id = GetId(ctx.name),
            Code = Get(ctx.code),
        };
    }

    private With Get(W5LogicParser.WithContext ctx)
    {
        return new With
        {
            Source = ctx,
            SourceFile = Source,
            Type = GetId(ctx.type),
            Name = GetId(ctx.name),
        };
    }

    private Step Get(W5LogicParser.StepContext ctx)
    {
        return new Step
        {
            Source = ctx,
            SourceFile = Source,
            Id = GetId(ctx.name),
            Code = Get(ctx.code),
        };
    }

    private EventTarget Get(W5LogicParser.EventTargetContext ctx)
    {
        return new EventTarget
        {
            Source = ctx,
            SourceFile = Source,
            TargetSpecifier = ctx.modifier is null ? null :
                ctx.modifier.Type switch
                {
                    W5LogicParser.KW_PHASE => TypeSpecifier.Phase,
                    W5LogicParser.KW_SCENE => TypeSpecifier.Scene,
                    _ => throw Error.WriteFatal(Source, ctx.Start, $"Unexpected token {ctx.modifier}"),
                },
            Target = GetId(ctx.name),
            Steps = { ctx._steps.Select(x => Get(x)) },
        };
    }

    private CodeBlock Get(W5LogicParser.CodeBlockContext ctx)
    {
        return new CodeBlock
        {
            Source = ctx,
            SourceFile = Source,
            Statements = { ctx.stmts is null ? [] : Get(ctx.stmts) },
        };
    }

    private IEnumerable<IStatement> Get(W5LogicParser.StatementListContext ctx)
    {
        foreach (var stmt in ctx._stmts)
            yield return Get(stmt);
    }

    private IStatement Get(W5LogicParser.StatementContext ctx)
    {
        switch (ctx)
        {
            case W5LogicParser.StmtExpressionContext node:
                return Get(node.expr);
            case W5LogicParser.StmtSpawnContext node:
                if (node.voting != null)
                    return new VotingSpawnStatement
                    {
                        Source = node,
                        SourceFile = Source,
                        Name = GetId(node.name),
                        With = { node._withs.Select(x => x.Text).Zip(node._expr.Select(x => Get(x))) }
                    };
                if (node.sequence != null)
                    return new SequenceSpawnStatement
                    {
                        Source = node,
                        SourceFile = Source,
                        Name = GetId(node.name),
                        Value = Get(node._expr[0]),
                    };
                if (node.@event != null)
                    return new AnyEventSpawnStatement
                    {
                        Source = node,
                        SourceFile = Source,
                    };
                throw Error.WriteFatal(Source, ctx.Start, "Invalid voting variant");
            case W5LogicParser.StmtNotifyPlayerContext node:
                return new NotifyPlayerStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                    Value = Get(node.exp),
                };
            case W5LogicParser.StmtNotifyContext node:
                return new NotifyStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Sequence = node.sequence is null ? null : GetId(node.sequence),
                    Name = GetId(node.name),
                };
            case W5LogicParser.StmtCondIfContext node:
                return new ConditionalIfStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Expression = Get(node.expr),
                    Success = { node.success is null ? [] : Get(node.success) },
                    Fail = { node.fail is null ? [] : Get(node.fail) },
                };
            case W5LogicParser.StmtLetContext node:
                return new LetStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                    Value = Get(node.expr),
                };
            default: throw Error.WriteFatal(Source, ctx.Start, $"Invalid type: {ctx.GetType()}");
        }
    }

    private IExpression Get(W5LogicParser.ExpressionContext ctx)
    {
        switch (ctx)
        {
            case W5LogicParser.ExprPipeContext node:
                return new PipeExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Left = Get(node.left),
                    Right = { node._right.Select(x => Get(x)) },
                };
            case W5LogicParser.ExprCompContext node:
                return new CompExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Left = Get(node.left),
                    Op = node.op.Type switch
                    {
                        W5LogicParser.OP_EQUAL => CompOp.Equal,
                        W5LogicParser.OP_UNEQUAL => CompOp.Unequal,
                        W5LogicParser.OP_GE => CompOp.GreaterOrEqual,
                        W5LogicParser.OP_LE => CompOp.LowerOrEqual,
                        W5LogicParser.OP_GT => CompOp.Greater,
                        W5LogicParser.OP_LT => CompOp.Lower,
                        _ => throw Error.WriteFatal(Source, node.op, $"Invalid token `{node.op.Text}`. Expected operator."),
                    },
                    Right = Get(node.right),
                };
            case W5LogicParser.ExprOrAndContext node:
                return node.op.Type switch
                {
                    W5LogicLexer.OP_OR => new OrExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Left = Get(node.left),
                        Right = Get(node.right),
                    },
                    W5LogicLexer.OP_AND => new AndExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Left = Get(node.left),
                        Right = Get(node.right),
                    },
                    _ => throw Error.WriteFatal(Source, node.op, $"Invalid operation type: {node.op.Text}"),
                };
            case W5LogicParser.ExprAddSubContext node:
                return node.op.Type switch
                {
                    W5LogicLexer.OP_ADD => new AddExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Left = Get(node.left),
                        Right = Get(node.right),
                    },
                    W5LogicLexer.OP_SUB => new SubExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Left = Get(node.left),
                        Right = Get(node.right),
                    },
                    _ => throw Error.WriteFatal(Source, node.op, $"Invalid operation type: {node.op.Text}"),
                };
            case W5LogicParser.ExprMulDivContext node:
                return node.op.Type switch
                {
                    W5LogicLexer.OP_MUL => new MulExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Left = Get(node.left),
                        Right = Get(node.right),
                    },
                    W5LogicLexer.OP_DIV => new DivExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Left = Get(node.left),
                        Right = Get(node.right),
                    },
                    _ => throw Error.WriteFatal(Source, node.op, $"Invalid operation type: {node.op.Text}"),
                };
            case W5LogicParser.ExprNegateContext node:
                return new NegateExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Value = Get(node.value),
                };
            case W5LogicParser.ExprGroupContext node:
                return new GroupExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Values = { node._expr.Select(x => Get(x)) },
                };
            case W5LogicParser.ExprIfLetContext node:
                return new IfLetStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                    Value = Get(node.expr),
                    Success = node.success is null ? null : Get(node.success),
                    Fail = node.fail is null ? null : Get(node.fail),
                };
            case W5LogicParser.ExprIfContext node:
                return new IfStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Expression = Get(node.expr),
                    Success = node.success is null ? null : Get(node.success),
                    Fail = node.fail is null ? null : Get(node.fail),
                };
            case W5LogicParser.ExprForLetContext node:
                return new ForLetStatement
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                    Value = Get(node.expr),
                    Loop = node.loop is null ? null : Get(node.loop),
                };
            case W5LogicParser.ExprGlobalContext node:
                return new GlobalExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                };
            case W5LogicParser.ExprVariableContext node:
                return new VariableExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                };
            case W5LogicParser.ExprCallContext node:
                if (node.open is null)
                    return new IdExpression
                    {
                        Source = node,
                        SourceFile = Source,
                        Name = GetId(node.name),
                    };
                return new CallExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Name = GetId(node.name),
                    Values = { node._args.Select(x => Get(x)) },
                };
            case W5LogicParser.ExprStringContext node:
                return new StringExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Value = GetId(node.value),
                };
            case W5LogicParser.ExprIntContext node:
                return new IntExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Value = GetId(node.value),
                };
            case W5LogicParser.ExprTypeContext node:
                return new TypedNameExpression
                {
                    Source = node,
                    SourceFile = Source,
                    Type = node.type.Type switch
                    {
                        W5LogicLexer.KW_MODE => NameType.Mode,
                        W5LogicLexer.KW_CHARACTER => NameType.Character,
                        W5LogicLexer.KW_WIN => NameType.Win,
                        W5LogicLexer.KW_PHASE => NameType.Phase,
                        W5LogicLexer.KW_SCENE => NameType.Scene,
                        W5LogicLexer.KW_VOTING => NameType.Voting,
                        W5LogicLexer.KW_SEQUENCE => NameType.Sequence,
                        W5LogicLexer.KW_EVENT => NameType.Event,
                        W5LogicLexer.KW_OPTION => NameType.Option,
                        W5LogicLexer.KW_LABEL => NameType.Label,
                        _ => throw Error.WriteFatal(Source, node.type, $"Invalid type: {node.type.Text}"),
                    },
                    Name = GetId(node.name),
                };
            default: throw Error.WriteFatal(Source, ctx.Start, $"Invalid type: {ctx.GetType()}");
        }
    }

    private PipeCall Get(W5LogicParser.PipeCallContext ctx)
    {
        return new PipeCall()
        {
            Source = ctx,
            SourceFile = Source,
            Name = GetId(ctx.name),
            Args = { ctx._expr.Select(x => Get(x)) },
        };
    }

    private Argument Get(W5LogicParser.ArgumentContext ctx)
    {
        return new Argument()
        {
            Source = ctx,
            SourceFile = Source,
            Name = ctx.name is null ? null : GetId(ctx.name),
            Value = Get(ctx.expr),
        };
    }

    private LabelTarget GetLabelTarget(IEnumerable<Antlr4.Runtime.IToken> tokens)
    {
        LabelTarget target = 0;
        foreach (var token in tokens)
        {
            target |= token.Type switch
            {
                W5LogicParser.KW_MODE => LabelTarget.Mode,
                W5LogicParser.KW_CHARACTER => LabelTarget.Character,
                W5LogicParser.KW_PHASE => LabelTarget.Phase,
                W5LogicParser.KW_SCENE => LabelTarget.Scene,
                W5LogicParser.KW_VOTING => LabelTarget.Voting,
                _ => throw Error.WriteFatal(Source, token, $"Unexpected token {token.ToString}"),
            };
        }
        return target;
    }
}
