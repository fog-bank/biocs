using System.Collections;
using System.Diagnostics;

namespace Biocs.Collections;

/// <summary>
/// Represents the collection of values in a dictionary, that can be accessed by index according by the specified list of keys.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
public class IndexedValueCollection<TKey, TValue> : IList<TValue?>, IReadOnlyList<TValue?> where TKey: notnull
{
    private readonly IDictionary<TKey, TValue> dictionary;
    private readonly IList<TKey> keyList;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedValueCollection{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
    /// <param name="keyList">The list of keys in order to associate values in <paramref name="dictionary"/> with index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="keyList"/> is <see langword="null"/>.</exception>
    public IndexedValueCollection(IDictionary<TKey, TValue> dictionary, IList<TKey> keyList)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(keyList);

        this.dictionary = dictionary;
        this.keyList = keyList;
    }

    /// <summary>
    /// Gets the value corresponding to the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the value to get.</param>
    /// <returns>The value corresponding to the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="index"/> is negative.</para> -or- <para><paramref name="index"/> is greater than <see cref="Count"/>.</para>
    /// </exception>
    public TValue? this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return dictionary.TryGetValue(keyList[index], out var value) ? value : default;
        }
    }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
    public int Count => keyList.Count;

    /// <inheritdoc/>
    public IEnumerator<TValue?> GetEnumerator() => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool Contains(TValue? item) => throw new NotImplementedException();

    /// <inheritdoc/>
    public int IndexOf(TValue? item) => throw new NotImplementedException();

    /// <inheritdoc/>
    [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
    public void CopyTo(TValue?[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        if (arrayIndex + Count > array.Length)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidCopyDestRange", Count, array.Length - arrayIndex));

        for (int i = 0; i < Count; i++)
            array[arrayIndex + i] = this[i];
    }

    #region Explicit Interface Implementations

    TValue? IList<TValue?>.this[int index]
    {
        get => this[index];
        [StringResourceUsage("NotSup.ReadOnlyCollection")]
        set => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));
    }

    bool ICollection<TValue?>.IsReadOnly => true;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<TValue?>.Add(TValue? item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList<TValue?>.Insert(int index, TValue? item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    bool ICollection<TValue?>.Remove(TValue? item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList<TValue?>.RemoveAt(int index) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<TValue?>.Clear() => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    #endregion
}
