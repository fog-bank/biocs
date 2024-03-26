using System.Collections;
using System.Diagnostics;

namespace Biocs.Collections;

/// <summary>
/// Represents the collection of values in a <see cref="IDictionary{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
public class IndexedValueCollection<TKey, TValue> : IList<TValue>, IReadOnlyList<TValue>
{
    private readonly IDictionary<TKey, TValue> dictionary;
    private readonly IList<TKey> keyList;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedValueCollection{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="keyList"></param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="keyList"/> is <see langword="null"/>.</exception>
    public IndexedValueCollection(IDictionary<TKey, TValue> dictionary, IList<TKey> keyList)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(keyList);

        this.dictionary = dictionary;
        this.keyList = keyList;
    }

    public TValue this[int index]
    {
        get
        {
            return dictionary[keyList[index]];
        }
    }

    /// <inheritdoc/>
    public int Count => keyList.Count;

    /// <inheritdoc/>
    public IEnumerator<TValue> GetEnumerator() => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool Contains(TValue item) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void CopyTo(TValue[] array, int arrayIndex) => throw new NotImplementedException();

    /// <inheritdoc/>
    public int IndexOf(TValue item) => throw new NotImplementedException();

    #region Explicit Interface Implementations

    TValue IList<TValue>.this[int index]
    {
        get => this[index];
        [StringResourceUsage("NotSup.ReadOnlyCollection")]
        set => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));
    }

    bool ICollection<TValue>.IsReadOnly => true;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList<TValue>.Insert(int index, TValue item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList<TValue>.RemoveAt(int index) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<TValue>.Clear() => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    #endregion
}
