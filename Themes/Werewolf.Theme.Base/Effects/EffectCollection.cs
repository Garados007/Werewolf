namespace Werewolf.Theme.Effects;

/// <summary>
/// The current collection of all <see cref="IEffect" /> that are assigned.
/// </summary>
/// <typeparam name="T">the effect kind</typeparam>
public class EffectCollection<T>
    where T : IEffect
{
    private readonly LinkedList<T> items = new();
    private readonly ReaderWriterLockSlim @lock = new();

    /// <summary>
    /// The total number of all effects in this collection
    /// </summary>
    public int Count => items.Count;

    /// <summary>
    /// This event is called every time when an event is added.
    /// </summary>
    public event Action<T>? Added;

    /// <summary>
    /// Add an effect to the collection
    /// </summary>
    /// <param name="item">the effect to add</param>
    /// <typeparam name="U">the type of the effect</typeparam>
    public void Add<U>(U item)
        where U : T
    {
        try
        {
            @lock.EnterWriteLock();
            var node = items.First;
            while (node is not null)
            {
                if (Equals(node.Value, item))
                {
                    node.Value = item;
                    return;
                }
                node = node.Next;
            }
            items.AddLast(item);
        }
        finally
        {
            @lock.ExitWriteLock();
            Added?.Invoke(item);
        }
    }

    /// <summary>
    /// Clears the whole collection
    /// </summary>
    public void Clear()
    {
        try
        {
            @lock.EnterWriteLock();
            items.Clear();
        }
        finally
        {
            @lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Checks if this specific effect is already contained in the collection
    /// </summary>
    /// <param name="item">the item to search for</param>
    /// <typeparam name="U">the type of the effect</typeparam>
    /// <returns>true if found</returns>
    public bool Contains<U>(U item)
        where U : T
    {
        try
        {
            @lock.EnterReadLock();
            return items.Contains(item);
        }
        finally
        {
            @lock.ExitReadLock();
        }
    }

    /// <summary>
    /// This event is called every time when an event is removed. This wont be called if the
    /// collection was cleared.
    /// </summary>
    public event Action<T>? Removed;

    /// <summary>
    /// Removes all effects of the specified type
    /// </summary>
    /// <typeparam name="U">the type of the effect to remove</typeparam>
    /// <returns>the number of effects removed</returns>
    public int Remove<U>()
    {
        try
        {
            @lock.EnterWriteLock();
            int count = 0;
            var node = items.First;
            while (node is not null)
            {
                var next = node.Next;
                if (node.Value is U)
                {
                    count++;
                    items.Remove(node);
                    Removed?.Invoke(node.Value);
                }
                node = next;
            }
            return count;
        }
        finally
        {
            @lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Remove a single effect from the collection
    /// </summary>
    /// <param name="item">the effect to remove</param>
    /// <typeparam name="U">the type of the effect</typeparam>
    /// <returns>true if removed</returns>
    public bool Remove<U>(U item)
        where U : T
    {
        try
        {
            @lock.EnterWriteLock();
            var success = items.Remove(item);
            if (success)
                Removed?.Invoke(item);
            return success;
        }
        finally
        {
            @lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get a single effect from this collection
    /// </summary>
    /// <typeparam name="U">the type that should be searched for</typeparam>
    /// <returns>the effect if found</returns>
    public U? GetEffect<U>()
    {
        try
        {
            @lock.EnterReadLock();
            foreach (var node in items)
            {
                if (node is U item)
                    return item;
            }
            return default;
        }
        finally
        {
            @lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Get a single effect from this collection.
    /// </summary>
    /// <param name="selector">Selects which of the <typeparamref name="U"/> should be used.</param>
    /// <typeparam name="U">the type that should be searched for</typeparam>
    /// <returns>the effect if found</returns>
    public U? GetEffect<U>(Func<U, bool> selector)
    {
        try
        {
            @lock.EnterReadLock();
            foreach (var node in items)
            {
                if (node is U item && selector(item))
                    return item;
            }
            return default;
        }
        finally
        {
            @lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Return all stored effects
    /// </summary>
    /// <returns>all effects</returns>
    public IEnumerable<T> GetEffects()
    {
        try
        {
            @lock.EnterReadLock();
            foreach (var node in items)
            {
                yield return node;
            }
        }
        finally
        {
            @lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Search for all effects in this collection that matches the given type
    /// </summary>
    /// <typeparam name="U">the type that should be searched for</typeparam>
    /// <returns>the found effects</returns>
    public IEnumerable<U> GetEffects<U>()
    {
        try
        {
            @lock.EnterReadLock();
            foreach (var node in items)
            {
                if (node is U item)
                    yield return item;
            }
        }
        finally
        {
            @lock.ExitReadLock();
        }
    }
}