namespace Tools.LangConfigGenerator;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Invalid args");
            Console.Error.WriteLine("dotnet run -- [path/to/config.json] [path/to/example.json]");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            var walker = new Walker();
            await walker.Walk(args[0], args[1]);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Environment.ExitCode = 2;
        }
    }
}