using LogicCompiler.Ast;

namespace LogicCompiler.Functions.AllFunctions;

internal static class Tools
{
    public static LabelNode? ExpectArgAsLabel(Context context, Argument arg)
    {
        if (arg.Value is not IdExpression and not TypedNameExpression)
        {
            Error.WriteError(arg, "This kind of expression is not allowed. You are only allowed to define the label name.");
            return null;
        }
        var type = arg.Value as TypedNameExpression ?? ((IdExpression?)arg.Value)?.CalculatedType;
        if (type is null || !context.Generator.Labels.TryGetValue(type.Name.Text, out var value))
            // already handled
            return null;
        return value;
    }

    public static string? ExpectName(Context context, Argument arg)
    {
        if (arg.Value is not IdExpression expr)
        {
            Error.WriteError(arg, "This kind of expression is not allowed. You are only allowed to specify a name.");
            return null;
        }
        return expr.Name.Text;
    }

    public static string? ExpectVariable(Context context, Argument arg)
    => arg.Value is null ? null : ExpectVariable(context, arg.Value);

    public static string? ExpectVariable(Context context, IExpression arg)
    {
        if (arg is not VariableExpression expr)
        {
            Error.WriteError(arg, "This kind of expression is not allowed. You are only allowed to specify a variable.");
            return null;
        }
        return expr.Name.Text;
    }
}
