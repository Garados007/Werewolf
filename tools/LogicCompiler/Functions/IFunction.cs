using LogicCompiler.Ast;

namespace LogicCompiler.Functions;

internal interface IFunction
{
    string Name { get; }
}

internal interface ICustomArgumentHandler : IFunction
{
}

internal interface ICallFunction : IFunction
{
    Ast.Type GetPreType(Id name, Context context, List<Ast.Argument> Args);

    void SetPostType(Id name, Context context, List<Ast.Argument> Args, Ast.Type expected);

    void Write(Output output, List<Ast.Argument> Args, Ast.Type expected);

    void WriteCallDoc(Output output);
}

internal interface IPipedFunction : IFunction
{
    Ast.Type GetPreType(Id name, Context context, Ast.Type consumedType, List<Ast.IExpression> Args);

    Ast.Type SetPostType(Id name, Context context, Ast.Type consumedType, List<Ast.IExpression> Args, Ast.Type expected);

    void Write(Output output, Ast.Type consumedType, List<Ast.IExpression> Args, Ast.Type expected);

    void WritePipedDoc(Output output);
}

internal interface IGlobal : IFunction
{
    Ast.Type GetPreType(Id name, Context context);

    void Write(Output output);

    void WriteGlobalDoc(Output output);
}

internal static class Registry
{
    public static Dictionary<string, ICallFunction> CallFunctions { get; } = [];

    public static Dictionary<string, IPipedFunction> PipedFunctions { get; } = [];

    public static Dictionary<string, IGlobal> Globals { get; } = [];

    static Registry()
    {
        Register(CallFunctions);
        Register(PipedFunctions);
        Register(Globals);
    }

    private static void Register<T>(Dictionary<string, T> dict)
        where T : IFunction
    {
        foreach (var type in typeof(T).Assembly.GetTypes()
            .Where(x => x.IsAssignableTo(typeof(T)) && !x.IsAbstract && x.IsClass))
        {
            if (Activator.CreateInstance(type) is not IFunction instance)
                continue;
            dict.Add(instance.Name, (T)instance);
        }
    }

    public static void WriteDoc(Output output)
    {
        output.WriteLine("# Functions");
        output.WriteLine();
        output.WriteLine("## Global collections");
        output.WriteLine();
        foreach (var (_, func) in Globals)
            func.WriteGlobalDoc(output);
        output.WriteLine("## Callable functions");
        output.WriteLine();
        foreach (var (_, func) in CallFunctions)
            func.WriteCallDoc(output);
        output.WriteLine("## Pipeable functions");
        output.WriteLine();
        foreach (var (_, func) in PipedFunctions)
            func.WritePipedDoc(output);
    }

    public static void WriteGlobalDoc(Output output, string signature, Ast.Type type, string doc)
    {
        output.WriteLine($"### `{signature}`");
        output.WriteLine();
        output.WriteLine(doc);
        output.WriteLine();
        output.WriteLine("| Info | Value |");
        output.WriteLine("|-|-|");
        output.Write($"| Return Type | ");
        type.WriteDoc(output);
        output.WriteLine(" |");
        output.WriteLine();
    }

    public static void WriteCallDoc(Output output, string signature, List<(string name, Ast.Type type)> args, Ast.Type returnType, string doc)
    {
        output.WriteLine($"### `{signature}`");
        output.WriteLine();
        output.WriteLine(doc);
        output.WriteLine();
        output.WriteLine("| Info | Value |");
        output.WriteLine("|-|-|");
        foreach (var (name, type) in args)
        {
            output.Write($"| Arg `{name}` | ");
            type.WriteDoc(output);
            output.WriteLine(" |");
        }
        output.Write($"| Return Type | ");
        returnType.WriteDoc(output);
        output.WriteLine(" |");
        output.WriteLine();
    }

    public static void WritePipeDoc(Output output, string signature, Ast.Type expectedType, List<(string name, Ast.Type type)> args, Ast.Type returnType, string doc)
    {
        output.WriteLine($"### `<col> | {signature}`");
        output.WriteLine();
        output.WriteLine(doc);
        output.WriteLine();
        output.WriteLine("| Info | Value |");
        output.WriteLine("|-|-|");
        output.Write($"| Pipe Input Type | ");
        expectedType.WriteDoc(output);
        output.WriteLine(" |");
        foreach (var (name, type) in args)
        {
            output.Write($"| Arg `{name}` | ");
            type.WriteDoc(output);
            output.WriteLine(" |");
        }
        output.Write($"| Return Type | ");
        returnType.WriteDoc(output);
        output.WriteLine(" |");
        output.WriteLine();
    }
}
