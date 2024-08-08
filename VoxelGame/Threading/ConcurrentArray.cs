using System.Collections;

namespace VoxelGame.Threading;

/// <summary>
/// Represents a thread-safe array that allows concurrent access to its elements.
/// </summary>
/// <typeparam name="T">The type of the elements in the array.</typeparam>
public class ConcurrentArray<T> : IEnumerable<T>
{
    private T[] _array;

    private readonly object _lock = new object();

    public ConcurrentArray(int size)
    {
        _array = new T[size];
    }

    public T this[int i]
    {
        get => Get(i);
        set => Set(i, value);
    }

    /// <summary>
    /// Gets the value at the specified index in the array.
    /// </summary>
    /// <param name="index">The index of the element to get.</param>
    /// <returns>The value at the specified index.</returns>
    public T Get(int index)
    {
        lock (_lock)
        {
            return _array[index];
        }
    }

    /// <summary>
    /// Sets the value of an element at the specified index in the array.
    /// </summary>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The value to set.</param>
    public void Set(int index, T value)
    {
        lock (_lock)
        {
            _array[index] = value;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_array).GetEnumerator();
    }
}