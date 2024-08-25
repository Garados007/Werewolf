
namespace LogicCompiler;

internal sealed class Output(Stream stream) : IDisposable, IAsyncDisposable
{
    private readonly Stream stream = stream;
    private readonly StreamWriter writer = new(stream);
    private int indent;
    private bool freshLine = true;

    public void Push()
    {
        indent++;
    }

    public void Pop()
    {
        indent = Math.Max(0, indent - 1);
    }

    public void Write(string piece)
    {
        if (piece.Length == 0)
            return;
        if (freshLine)
        {
            for (int i = 0; i < indent; i++)
            {
                writer.Write("    ");
            }
            freshLine = false;
        }
        writer.Write(piece);
    }

    public void WriteLine()
    {
        writer.WriteLine();
        freshLine = true;
    }

    public void WriteLine(string piece)
    {
        Write(piece);
        WriteLine();
    }

    public void WriteBlockBegin()
    {
        if (!freshLine)
            WriteLine();
        WriteLine("{");
        Push();
    }
    public void WriteBlockEnd()
    {
        if (!freshLine)
            WriteLine();
        Pop();
        WriteLine("}");
    }

    public void WriteCommaSeparatedList(string left, IEnumerable<string> list, string right)
    {
        Write(left);
        if (WriteCommaSeparatedList(list, true))
            Write(" ");
        Write(right);
    }

    public bool WriteCommaSeparatedList(IEnumerable<string> list, bool spaceFirst = false)
    {
        bool first = true;
        foreach (var item in list)
        {
            if (first)
            {
                first = false;
                if (spaceFirst)
                    Write(" ");
            }
            else Write(", ");
            Write(item);
        }
        return !first;
    }

    public void Dispose()
    {
        writer.Flush();
        stream.SetLength(stream.Position);
        ((IDisposable)writer).Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await writer.FlushAsync();
        stream.SetLength(stream.Position);
        await writer.DisposeAsync();
    }
}
