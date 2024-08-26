using System.Collections;

namespace Werewolf.Theme.Labels;

/// <summary>
/// The current collection of all <see cref="ILabel" /> that are assigned.
/// </summary>
/// <typeparam name="THostLabel">the effect kind</typeparam>
public class LabelCollection<THostLabel> : IEnumerable<THostLabel>
    where THostLabel : class, ILabel
{
    private readonly ThreadsafeCollection<THostLabel> items = [];

    /// <summary>
    /// The total number of all effects in this collection
    /// </summary>
    public int Count => items.Count;

    /// <summary>
    /// This event is called every time when an event is added.
    /// </summary>
    public event Action<THostLabel>? Added;

    /// <summary>
    /// Add an effect to the collection
    /// </summary>
    /// <param name="item">the effect to add</param>
    /// <typeparam name="TLabel">the type of the effect</typeparam>
    public TLabel Add<TLabel>(TLabel item)
        where TLabel : THostLabel
    {
        items.Add(item);
        Added?.Invoke(item);
        return item;
    }

    public THostLabel? Add(Type labelType)
    {
        var label = Activator.CreateInstance(labelType) as THostLabel;
        return label is not null ? Add(label) : default;
    }

    /// <summary>
    /// Clears the whole collection
    /// </summary>
    public void Clear()
    {
        items.Clear();
        Removed?.Invoke(null);
    }

    /// <summary>
    /// Checks if this specific effect is already contained in the collection
    /// </summary>
    /// <param name="item">the item to search for</param>
    /// <typeparam name="TLabel">the type of the effect</typeparam>
    /// <returns>true if found</returns>
    public bool Contains<TLabel>(TLabel item)
        where TLabel : THostLabel
    {
        return items.Contains(item);
    }

    /// <summary>
    /// Checks if any effect with the type <typeparamref name="TLabel"/> is contained in the collection.
    /// </summary>
    /// <typeparam name="TLabel">the type of the effect</typeparam>
    /// <returns>true if found</returns>
    public bool Contains<TLabel>()
        where TLabel : THostLabel
    => GetEffect<TLabel>() is not null;

    /// <summary>
    /// This event is called every time when an event is removed. This wont be called if the
    /// collection was cleared.
    /// </summary>
    public event Action<THostLabel?>? Removed;

    /// <summary>
    /// Removes all effects of the specified type
    /// </summary>
    /// <typeparam name="TLabel">the type of the effect to remove</typeparam>
    /// <returns>the number of effects removed</returns>
    public int Remove<TLabel>()
        where TLabel : THostLabel
    {
        List<THostLabel>? publish = Removed is null ? null : [];
        int removed = items.RemoveAll(x => x is TLabel, publish);
        publish?.ForEach(x => Removed?.Invoke(x));
        return removed;
    }

    public int Remove(Type type)
    {
        List<THostLabel>? publish = Removed is null ? null : [];
        int removed = items.RemoveAll(x => x.GetType() == type, publish);
        publish?.ForEach(x => Removed?.Invoke(x));
        return removed;
    }

    /// <summary>
    /// Remove a single effect from the collection
    /// </summary>
    /// <param name="item">the effect to remove</param>
    /// <typeparam name="TLabel">the type of the effect</typeparam>
    /// <returns>true if removed</returns>
    public bool Remove<TLabel>(TLabel item)
        where TLabel : THostLabel
    {
        if (!items.Remove(item))
            return false;
        Removed?.Invoke(item);
        return true;
    }

    /// <summary>
    /// Get a single effect from this collection
    /// </summary>
    /// <typeparam name="TLabel">the type that should be searched for</typeparam>
    /// <returns>the effect if found</returns>
    public TLabel? GetEffect<TLabel>()
        where TLabel : THostLabel
    {
        foreach (var node in items)
        {
            if (node is TLabel item)
                return item;
        }
        return default;
    }

    /// <summary>
    /// Get a single effect from this collection.
    /// </summary>
    /// <param name="selector">Selects which of the <typeparamref name="TLabel"/> should be used.</param>
    /// <typeparam name="TLabel">the type that should be searched for</typeparam>
    /// <returns>the effect if found</returns>
    public TLabel? GetEffect<TLabel>(Func<TLabel, bool> selector)
        where TLabel : THostLabel
    {
        foreach (var node in items)
        {
            if (node is TLabel item && selector(item))
                return item;
        }
        return default;
    }

    /// <summary>
    /// Return all stored effects
    /// </summary>
    /// <returns>all effects</returns>
    public IEnumerable<THostLabel> GetEffects()
    {
        foreach (var node in items)
        {
            yield return node;
        }
    }

    /// <summary>
    /// Search for all effects in this collection that matches the given type
    /// </summary>
    /// <typeparam name="TLabel">the type that should be searched for</typeparam>
    /// <returns>the found effects</returns>
    public IEnumerable<TLabel> GetEffects<TLabel>()
    {
        foreach (var node in items)
        {
            if (node is TLabel item)
                yield return item;
        }
    }

    public IEnumerator<THostLabel> GetEnumerator()
    {
        foreach (var node in items)
        {
            yield return node;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
