using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace LogicCompiler;

public class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
{
    private static void SetInfo<T>(JsonTypeInfo info)
    {
        var baseType = typeof(T);
        if (info.Type != baseType)
            return;
        info.PolymorphismOptions = new JsonPolymorphismOptions
        {
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };
        var list = baseType.Assembly.GetTypes()
            .Where(x => !x.IsAbstract && x.IsAssignableTo(baseType))
            .Select(x => new JsonDerivedType(x, x.Name));
        foreach (var type in list)
            info.PolymorphismOptions.DerivedTypes.Add(type);
    }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

        SetInfo<Ast.IStatement>(jsonTypeInfo);
        SetInfo<Ast.IExpression>(jsonTypeInfo);

        return jsonTypeInfo;
    }
}
