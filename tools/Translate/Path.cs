using System;

namespace Translate
{
    public class Path
    {
        public ReadOnlyMemory<string> Parts { get; }

        public Path()
            => Parts = ReadOnlyMemory<string>.Empty;

        public Path(ReadOnlyMemory<string> parts)
            => Parts = parts;
        
        public static Path operator +(Path path, string suffix)
        {
            Memory<string> @new = new string[path.Parts.Length + 1];
            path.Parts.CopyTo(@new[..path.Parts.Length]);
            @new.Span[@new.Length - 1] = suffix;
            return new Path(@new);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Path path || path.Parts.Length != Parts.Length)
                return false;
            var localSpan = Parts.Span;
            var otherSpan = path.Parts.Span;
            for (int i = 0; i < Parts.Length; ++i)
                if (localSpan[i] != otherSpan[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            var code = new HashCode();
            var span = Parts.Span;
            for (int i = 0; i < span.Length; ++i)
                code.Add(span[i]);
            return code.ToHashCode();
        }

        public static bool operator ==(Path left, Path right)
            => left.Equals(right);
        
        public static bool operator !=(Path left, Path right)
            => !left.Equals(right);

        public string this[int index] => Parts.Span[index];

        public string this[Index index] => Parts.Span[index];

        public Path this[Range range] => new Path(Parts[range]);

        public static implicit operator Path(string singleSegment)
            => new Path(new [] { singleSegment });

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Parts.Length; ++i)
            {
                if (i > 0)
                    sb.Append('.');
                sb.Append(Parts.Span[i]);
            }
            return sb.ToString();
        }
    }
}