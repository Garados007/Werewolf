namespace LogicCompiler;

internal static class Extensions
{
    public static void Add<T>(this List<T> list, IEnumerable<T> elements)
    {
        list.AddRange(elements);
    }

    public static void Add<K, V>(this Dictionary<K, V> dict, IEnumerable<(K, V)> elements)
        where K : notnull
    {
        foreach (var (k, v) in elements)
            _ = dict.TryAdd(k, v);
    }

    public static T? At<T>(this List<T> list, int index)
        where T : notnull
    {
        return index < 0 || index >= list.Count ? default : list[index];
    }

    public static T? OptionalFirstOrError<T>(this IEnumerable<T> list)
        where T : notnull
    {
        T? first = default;
        bool hasFirst = false;
        foreach (var element in list)
        {
            if (hasFirst)
            {
                throw new InvalidDataException("Has more than one element");
            }
            hasFirst = true;
            first = element;
        }
        return first;
    }

    public static Ast.Func? Get<T>(this T container, string name)
        where T : Ast.ICodeContainer
    {
        return container.Funcs.Find(x => x.Id.Text == name);
    }
}
