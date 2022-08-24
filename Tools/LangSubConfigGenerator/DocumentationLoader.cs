using System.Text.RegularExpressions;
using System.Xml;
using System.Reflection;

namespace LangSubConfigGenerator;

// this is largely inspired by https://github.com/ZacharyPatten/Towel/blob/f358136cdb65eb99c7a3ffd00416a7e8ee4cd539/Sources/Towel/Meta.cs
// and https://docs.microsoft.com/en-us/archive/msdn-magazine/2019/october/csharp-accessing-xml-documentation-via-reflection
public class DocumentationLoader
{
    private readonly Dictionary<string, string> documents = new();

    public string FilePath { get; }

    public DocumentationLoader(string path)
    {
        FilePath = path;
    }

    public async Task Load()
    {
        var doc = new XmlDocument();
        doc.LoadXml(await File.ReadAllTextAsync(FilePath));
        foreach (XmlElement member in doc["doc"]!["members"]!.GetElementsByTagName("member"))
        {
            var name = member.GetAttribute("name");
            var summary = member["summary"]?.InnerText.Trim();
            if (summary != null)
                documents[name] = summary;
        }
    }

    private static string XmlDocumentationKeyHelper(
        string? typeFullNameString,
        string? memberNameString
    )
    {
        string key = Regex.Replace(
            typeFullNameString ?? "", @"\[.*\]",
            string.Empty
        ).Replace('+', '.');
        if (memberNameString != null)
        {
            key += "." + memberNameString;
        }
        return key;
    }

    public string? GetDocumentation(Type type)
    {
        string key = "T:" + XmlDocumentationKeyHelper(type.FullName, null);
        return documents.TryGetValue(key, out string? documentation) ? documentation : null;
    }

    public string? GetDocumentation(PropertyInfo propertyInfo)
    {
        string key = "P:" + XmlDocumentationKeyHelper(
            propertyInfo.DeclaringType?.FullName, propertyInfo.Name);
        return documents.TryGetValue(key, out string? documentation) ? documentation : null;
    }

    public string? GetDocumentation(MethodInfo methodInfo)
    {
        var key = GetMethodBaseKey(methodInfo);
        if (key is null)
            return null;
        return documents.TryGetValue(key, out string? documentation) ? documentation : null;
    }

    public string? GetDocumentation(ConstructorInfo constructorInfo)
    {
        var key = GetMethodBaseKey(constructorInfo);
        if (key is null)
            return null;
        return documents.TryGetValue(key, out string? documentation) ? documentation : null;
    }

    private static string? GetMethodBaseKey(MethodBase method)
    {
        var isConstructor = method is ConstructorInfo;
        
        if (method.DeclaringType is null)
        {
            throw new ArgumentException($"{method.GetType()}.{nameof(method.DeclaringType)} is null");
        }

        if (!isConstructor && method.DeclaringType.IsGenericType)
        {
            method = method.DeclaringType.GetGenericTypeDefinition().GetMethods(
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.NonPublic
            ).First(x => x.MetadataToken == method.MetadataToken);
        }

        var typeGenericMap = new Dictionary<string, int>();
        var typeGenericArguments = method.DeclaringType!.GetGenericArguments();
        for (int i = 0; i < typeGenericArguments.Length; ++i)
        {
            typeGenericMap[typeGenericArguments[i].Name] = i;
        }

        var methodGenericMap = new Dictionary<string, int>();
        if (!isConstructor)
        {
            var methodGenericArguments = method.GetGenericArguments();
            for (int i = 0; i < methodGenericArguments.Length; ++i)
            {
                methodGenericMap[methodGenericArguments[i].Name] = i;
            }
        }

        var parameterInfos = method.GetParameters();


		string memberTypePrefix = "M:";
		string declarationTypeString = GetXmlDocumenationFormattedString(method.DeclaringType, false, typeGenericMap, methodGenericMap);
		string memberNameString =
			isConstructor ? "#ctor" :
			method.Name;
		string methodGenericArgumentsString =
			methodGenericMap.Count > 0 ?
			"``" + methodGenericMap.Count :
			string.Empty;
		string parametersString =
			parameterInfos.Length > 0 ?
			"(" + string.Join(",", method.GetParameters().Select(x => GetXmlDocumenationFormattedString(x.ParameterType, true, typeGenericMap, methodGenericMap))) + ")" :
			string.Empty;

		string key =
			memberTypePrefix +
			declarationTypeString +
			"." +
			memberNameString +
			methodGenericArgumentsString +
			parametersString;
        

		if (!isConstructor && (method.Name is "op_Implicit" || method.Name is "op_Explicit"))
		{
			key += "~" + GetXmlDocumenationFormattedString(((MethodInfo)method).ReturnType, true, typeGenericMap, methodGenericMap);
		}
		return key;
    }

    internal static string GetXmlDocumenationFormattedString(
		Type type,
		bool isMethodParameter,
		Dictionary<string, int> typeGenericMap,
		Dictionary<string, int> methodGenericMap)
	{
		if (type.IsGenericParameter)
		{
			return methodGenericMap.TryGetValue(type.Name, out int methodIndex)
				? "``" + methodIndex
				: "`" + typeGenericMap[type.Name];
		}
		else if (type.HasElementType)
		{
			string elementTypeString = GetXmlDocumenationFormattedString(
				type.GetElementType() ?? throw new ArgumentException($"{nameof(type)}.{nameof(Type.HasElementType)} && {nameof(type)}.{nameof(Type.GetElementType)}() is null", nameof(type)),
				isMethodParameter,
				typeGenericMap,
				methodGenericMap);

			switch (type)
			{
				case Type when type.IsPointer:
					return elementTypeString + "*";

				case Type when type.IsByRef:
					return elementTypeString + "@";

				case Type when type.IsArray:
					int rank = type.GetArrayRank();
					string arrayDimensionsString = rank > 1
						? "[" + string.Join(",", Enumerable.Repeat("0:", rank)) + "]"
						: "[]";
					return elementTypeString + arrayDimensionsString;

				default:
					throw new Exception($"{nameof(GetXmlDocumenationFormattedString)} encountered an unhandled element type: {type}");
			}
		}
		else
		{
			string prefaceString = type.IsNested
				? GetXmlDocumenationFormattedString(
					type.DeclaringType ?? throw new ArgumentException($"{nameof(type)}.{nameof(Type.IsNested)} && {nameof(type)}.{nameof(Type.DeclaringType)} is null", nameof(type)),
					isMethodParameter,
					typeGenericMap,
					methodGenericMap) + "."
				: type.Namespace + ".";

			string typeNameString = isMethodParameter
				? typeNameString = Regex.Replace(type.Name, @"`\d+", string.Empty)
				: typeNameString = type.Name;

			string genericArgumentsString = type.IsGenericType && isMethodParameter
				? "{" + string.Join(",",
					type.GetGenericArguments().Select(argument =>
						GetXmlDocumenationFormattedString(
							argument,
							isMethodParameter,
							typeGenericMap,
							methodGenericMap))
					) + "}"
				: string.Empty;

			return prefaceString + typeNameString + genericArgumentsString;
		}
	}
}