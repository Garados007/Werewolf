namespace LogicCompiler.Ast;

internal sealed class Context
{
    public Context? Parent { get; }

    public Generator Generator { get; }

    public Dictionary<string, VariableInfo> Variables { get; } = [];

    public Context(Generator generator)
    {
        Generator = generator;
    }

    public Context(Context parent)
    {
        Parent = parent;
        Generator = parent.Generator;
    }

    public VariableInfo? Get(string name)
    {
        var result = Variables.TryGetValue(name, out var info) ? info : Parent?.Get(name);
        return result is null || result.Deleted ? null : result;
    }

    public void Delete(string name)
    {
        Variables[name] = new VariableInfo(name, ValueType.None, null)
        {
            Deleted = true,
        };
    }

    public void Add(string name, Type type)
    {
        Variables.Add(name, new VariableInfo(name, type, null));
    }

    public void Add(LetStatement statement)
    {
        var type = statement.Value?.PreType.Flag == ValueType.None ?
            statement.Value?.GetPreType(this) : statement.Value?.PreType;
        if (Get(statement.Name.Text) is VariableInfo info)
        {
            if (info.Type != type)
            {
                Error.WriteError(statement.Name, $"Cannot redefine variable {statement.Name.Text} with new type {type} when former type was {info.Type}.");
                return;
            }
            if (info.Definition is not LetStatement)
            {
                Error.WriteError(statement.Name, $"Cannot redefine variable {statement.Name.Text}");
                return;
            }
            statement.Redefinition = true;
        }
        else
        {
            Variables.Add(statement.Name.Text,
                new VariableInfo(statement.Name.Text, type ?? ValueType.Void, statement));
        }
    }

    public void Add(IfLetStatement statement)
    {
        var type = (statement.Value?.PreType.Flag == ValueType.None ?
            statement.Value?.GetPreType(this) : statement.Value?.PreType) ?? ValueType.Void;
        if (Get(statement.Name.Text) is not null)
            Error.WriteError(statement.Name, $"Cannot redefine variable {statement.Name.Text}");
        Variables.Add(statement.Name.Text, new VariableInfo(
            statement.Name.Text,
            new Type(type.Flag & ~ValueType.Optional, type.CollectionDepth),
            statement));
    }

    public void Add(ForLetStatement statement)
    {
        var type = (statement.Value?.PreType.Flag == ValueType.None ?
            statement.Value?.GetPreType(this) : statement.Value?.PreType) ?? ValueType.Void;
        type = type.CollectionDepth <= 1 ? type.Flag & ~ValueType.Collection :
            new Type(type.Flag, type.CollectionDepth - 1);
        if (Get(statement.Name.Text) is not null)
            Error.WriteError(statement.Name, $"Cannot redefine variable {statement.Name.Text}");
        Variables.Add(statement.Name.Text, new VariableInfo(
            statement.Name.Text,
            type,
            statement));
    }

    public void Check()
    {
        foreach (var (_, variable) in Variables)
            variable.Check();
    }

    public void Flatten(Dictionary<string, Type> target)
    {
        Parent?.Flatten(target);
        foreach (var (name, info) in Variables)
            if (info.Deleted)
                _ = target.Remove(name);
            else target[name] = info.Type;
    }
}

internal sealed record VariableInfo(
    string Name,
    Type Type,
    ISourceNode? Definition
)
{
    public List<IStatement> Use { get; set; } = [];

    public bool Deleted { get; set; }

    public void Check()
    {
        if (Definition is null || Use.Count > 0)
            return;
        Error.WriteWarning(Definition, $"Variable {Name} is never used and this statement can be removed.");
    }
};
