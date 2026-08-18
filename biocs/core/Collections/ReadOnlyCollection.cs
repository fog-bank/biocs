using System.Collections;
using System.Diagnostics;

namespace Biocs.Collections;

[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
internal sealed class ReadOnlyCollection<T>(ICollection<T> collection) : ICollection<T>, IReadOnlyCollection<T>, ICollection
{
    public int Count => collection.Count;

    public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();

    public void CopyTo(T[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);

    public bool Contains(T item) => collection.Contains(item);

    #region Explicit Interface Implementations

    bool ICollection<T>.IsReadOnly => true;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();

    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        try
        {
            int i = 0;
            foreach (var item in collection)
                array.SetValue(item, index + i++);
        }
        catch (Exception ex)
            when (ex is ArgumentException or ArrayTypeMismatchException or IndexOutOfRangeException or InvalidCastException)
        {
            throw new ArgumentException(null, nameof(array), ex);
        }
    }

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<T>.Add(T item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<T>.Clear() => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    #endregion
}
