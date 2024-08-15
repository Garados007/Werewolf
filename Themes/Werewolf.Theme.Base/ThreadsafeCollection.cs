using System.Collections;

namespace Werewolf.Theme;

public sealed class ThreadsafeCollection<T> : ICollection<T>
    where T : notnull
{
    private sealed class Node(T value)
    {
        public T Value { get; set; } = value;

        public Node? Next { get; set; }
    }

    private int count;
    private Node? head;
    private Node? tail;
    public int Count => count;

    bool ICollection<T>.IsReadOnly => false;

    private SpinLock modifyLock;

    public void Add(T item)
    {
        bool lockTaken = false;
        while (!lockTaken)
        {
            modifyLock.TryEnter(ref lockTaken);
            if (!lockTaken)
                Thread.Sleep(1);
        }
        try
        {
            var node = new Node(item);
            _ = Interlocked.Add(ref count, 1);
            if (tail is null)
            {
                tail = node;
                head = node;
            }
            else
            {
                tail.Next = node;
                tail = node;
            }
        }
        finally
        {
            modifyLock.Exit();
        }
    }

    public void Clear()
    {
        bool lockTaken = false;
        while (!lockTaken)
        {
            modifyLock.TryEnter(ref lockTaken);
            if (!lockTaken)
                Thread.Sleep(1);
        }
        try
        {
            count = 0;
            tail = null;
            head = null;
        }
        finally
        {
            modifyLock.Exit();
        }
    }

    public bool Contains(T item)
    {
        foreach (var e in this)
            if (e.Equals(item))
                return true;
        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex] = item;
            arrayIndex++;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        var current = head;
        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    public bool Remove(T item)
    {
        Node? prev = null;
        var current = head;
        while (current != null)
        {
            if (current.Value.Equals(item))
            {
                bool lockTaken = false;
                while (!lockTaken)
                {
                    modifyLock.TryEnter(ref lockTaken);
                    if (!lockTaken)
                        Thread.Sleep(1);
                }
                try
                {
                    if (current == tail)
                        tail = prev;
                    if (prev is null)
                        head = current.Next;
                    else
                        prev.Next = current.Next;
                    _ = Interlocked.Add(ref count, -1);
                    return true;
                }
                finally
                {
                    modifyLock.Exit();
                }
            }
            prev = current;
            current = current.Next;
        }
        return false;
    }

    public int RemoveAll(Func<T, bool> check, ICollection<T>? removedList = null)
    {
        Node? prev = null;
        var current = head;
        int removed = 0;
        while (current != null)
        {
            if (check(current.Value))
            {
                bool lockTaken = false;
                while (!lockTaken)
                {
                    modifyLock.TryEnter(ref lockTaken);
                    if (!lockTaken)
                        Thread.Sleep(1);
                }
                try
                {
                    if (current == tail)
                        tail = prev;
                    if (prev is null)
                        head = current.Next;
                    else
                        prev.Next = current.Next;
                    _ = Interlocked.Add(ref count, -1);
                    removed++;
                    removedList?.Add(current.Value);
                }
                finally
                {
                    modifyLock.Exit();
                }
            }
            prev = current;
            current = current.Next;
        }
        return removed;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
