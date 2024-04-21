using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Biocs.Collections;

/// <summary>
/// Represents the collection of values in a dictionary, that can be accessed by index according by the specified list of keys.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <seealso cref="SortedList{TKey, TValue}"/>
/// <seealso cref="System.Collections.ObjectModel.KeyedCollection{TKey, TItem}"/>
[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
public class IndexedValueCollection<TKey, TValue> : IList<TValue?>, IReadOnlyList<TValue?>, IList where TKey : notnull
{
    private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
    private readonly IReadOnlyList<TKey> keyList;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedValueCollection{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
    /// <param name="keyList">The list of keys in order to associate values in <paramref name="dictionary"/> with index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="keyList"/> is <see langword="null"/>.</exception>
    public IndexedValueCollection(IReadOnlyDictionary<TKey, TValue> dictionary, IReadOnlyList<TKey> keyList)
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
    /// <returns>
    /// The value corresponding to the specified index. If the wrapped dictionary does not contain the key corresponding to the specified index,
    /// the method returns the <see langword="default"/> value for <typeparamref name="TValue"/>.
    /// </returns>
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
    public IEnumerator<TValue?> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    /// <summary>
    /// Copies the entire collection to an existing one-dimensional <see cref="Array"/>.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the values.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
    /// <exception cref="ArgumentException">
    /// The number of values in the collection is greater than the available space from <paramref name="arrayIndex"/>
    /// to the end of <paramref name="array"/>.
    /// </exception>
    [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
    public void CopyTo(TValue?[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        if (arrayIndex + Count > array.Length)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidCopyDestRange", Count, array.Length - arrayIndex));

        var span = array.AsSpan(arrayIndex, Count);

        for (int i = 0; i < span.Length; i++)
            span[i] = this[i];
    }

    /// <summary>
    /// Determines whether the collection contains the specified value.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> is found in the collection; otherwise, <see langword="false"/>.</returns>
    public bool Contains(TValue? item) => IndexOf(item) >= 0;

    /// <summary>
    /// Determines the index of the specified value in the collection.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <returns>The index of <paramref name="item"/> if found in the collection; otherwise, -1.</returns>
    public int IndexOf(TValue? item)
    {
        var comparer = EqualityComparer<TValue>.Default;

        for (int i = 0; i < Count; i++)
        {
            if (comparer.Equals(this[i], item))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Tries to get the value corresponding to the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the value to get.</param>
    /// <param name="item">
    /// When this method returns, the value corresponding to the specified index, if the wrapped dictionary contains the key corresponding to
    /// the specified index; otherwise, the <see langword="default"/> value for <typeparamref name="TValue"/>.</param>
    /// <returns><see langword="true"/> if the wrapped dictionary contains the key corresponding to the specified index; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="index"/> is negative.</para> -or- <para><paramref name="index"/> is greater than <see cref="Count"/>.</para>
    /// </exception>
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out TValue item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return dictionary.TryGetValue(keyList[index], out item);
    }

    #region Explicit Interface Implementations

    TValue? IList<TValue?>.this[int index]
    {
        get => this[index];
        [StringResourceUsage("NotSup.ReadOnlyCollection")]
        set => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));
    }

    object? IList.this[int index]
    {
        get => this[index];
        [StringResourceUsage("NotSup.ReadOnlyCollection")]
        set => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));
    }

    bool ICollection<TValue?>.IsReadOnly => true;

    bool IList.IsReadOnly => true;

    bool IList.IsFixedSize => true;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        try
        {
            for (int i = 0; i < Count; i++)
                array.SetValue(this[i], index + i);
        }
        catch (Exception ex) when (ex is ArgumentException or ArrayTypeMismatchException or IndexOutOfRangeException or InvalidCastException)
        {
            throw new ArgumentException(null, nameof(array), ex);
        }
    }

    bool IList.Contains(object? value) => value switch
    {
        TValue item => Contains(item),
        null when default(TValue) == null => Contains(default),
        _ => false
    };

    int IList.IndexOf(object? value) => value switch
    {
        TValue item => IndexOf(item),
        null when default(TValue) == null => IndexOf(default),
        _ => -1
    };

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<TValue?>.Add(TValue? item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    int IList.Add(object? value) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList<TValue?>.Insert(int index, TValue? item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList.Insert(int index, object? value) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    bool ICollection<TValue?>.Remove(TValue? item) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList.Remove(object? value) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList<TValue?>.RemoveAt(int index) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList.RemoveAt(int index) => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void ICollection<TValue?>.Clear() => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    [StringResourceUsage("NotSup.ReadOnlyCollection")]
    void IList.Clear() => throw new NotSupportedException(Res.GetString("NotSup.ReadOnlyCollection"));

    #endregion
}
