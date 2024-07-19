using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;

namespace Werewolf.User;

public readonly struct UserId
{
    public ObjectId Id { get; }

    public UserId(string id)
        => Id = ObjectId.Parse(id ?? throw new ArgumentNullException(nameof(id)));

    public UserId(ObjectId id)
        => Id = id;

    public UserId(ReadOnlySpan<byte> bytes)
        => Id = new ObjectId(bytes.ToArray());

    public static bool TryParse(string value, [NotNullWhen(true)] out UserId? id)
    {
        if (ObjectId.TryParse(value, out ObjectId objectId))
        {
            id = new UserId(objectId);
            return true;
        }
        else
        {
            id = default;
            return false;
        }
    }

    public override string ToString()
        => Id.ToString();

    public override bool Equals(object? obj)
    {
        return obj is UserId id &&
               Id == id.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static implicit operator string(UserId id)
        => id.ToString();

    public static explicit operator UserId(string id)
        => new(id);

    public static implicit operator string?(UserId? id)
        => id?.ToString();

    public static explicit operator UserId?(string? id)
        => id is null ? null : (UserId?)new UserId(id);


    public static bool operator ==(UserId left, UserId right)
        => left.Id == right.Id;

    public static bool operator !=(UserId left, UserId right)
        => left.Id != right.Id;
}
