namespace LangConv.Validation;

internal interface IValidator
{
    void Check(Data data);

    Task CheckAsync(Data data)
    {
        return Task.Run(() => Check(data));
    }

    static async Task<bool> CheckAllAsync(Data data)
    {
        var handler = new List<Task>();
        var validator = typeof(IValidator);
        foreach (var type in validator.Assembly.GetTypes())
            if (!type.IsAbstract && !type.IsInterface && type.IsAssignableTo(validator))
            {
                var handle = Activator.CreateInstance(type) as IValidator;
                if (handle is not null)
                    handler.Add(handle.CheckAsync(data));
            }
        await Task.WhenAll(handler);
        return !Log.HasError;
    }
}

internal static class Log
{
    public static bool HasError { get; private set; }

    public static void Warning(IValidator source, string message)
    {
        Console.Error.WriteLine($"WARN: {source.GetType().Name}: {message}");
    }

    public static void Error(IValidator source, string message)
    {
        Console.Error.WriteLine($"ERROR: {source.GetType().Name}: {message}");
        HasError = true;
    }

    public static AbortException Fatal(IValidator source, string message)
    {
        Error(source, message);
        return new AbortException(AbortCode.ValidationError, message);
    }
}
