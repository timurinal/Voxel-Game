using System.Collections;

namespace VoxelGame.Threading;

public class ConcurrentList<T> : IEnumerable<T>, ICollection<T>
{
    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    public int Count => _length;
    public bool IsReadOnly { get; }

    private T[] _array;

    private int _length;

    private readonly object _lock = new object();

    public ConcurrentList()
    {
        _array = [];
        _length = 0;
    }

    public ConcurrentList(ICollection<T> collection)
    {
        _array = new T[collection.Count];
        collection.CopyTo(_array, 0);
        _length = collection.Count;
    }
    
    public T Get(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException("Index is out of range");
            
            return _array[index];
        }
    }

    public void Add(T value)
    {
        lock (_lock)
        {
            _length++;
            Array.Resize(ref _array, _length);
            _array[^1] = value;
        }
    }

    public void Remove(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException("Index is out of range");

            for (int i = index; i < _length - 1; i++)
            {
                _array[i] = _array[i + 1];
            }

            _length--;
            Array.Resize(ref _array, _length);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _length = 0;
            _array = [];
        }
    }

    public bool Contains(T item)
    {
        lock (_lock)
        {
            // TODO: maybe add a faster searching algorithm
            foreach (var i in _array)
            {
                if (EqualityComparer<T>.Default.Equals(i, item))
                    return true;
            }

            return false;
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new IndexOutOfRangeException("Array Index is out of range.");

        if (array.Length - arrayIndex < _length)
            throw new ArgumentException(
                "The destination array cannot fit the amount of elements in this collection");

        lock (_lock)
        {
            Array.Copy(_array, 0, array, arrayIndex, _length);
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