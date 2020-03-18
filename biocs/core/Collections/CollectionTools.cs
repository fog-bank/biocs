using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Biocs.Collections
{
    /// <summary>
    /// Provides static methods for collections.
    /// </summary>
    public static class CollectionTools
    {
        /// <summary>
        /// Determines whether all items in the specified collection are equal.
        /// </summary>
        /// <typeparam name="T">The type of items of <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The <see cref="IEnumerable{T}"/> to check equality between items.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="collection"/> is not empty and all items are equal;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
        public static bool AllItemsAreEqual<T>(this IEnumerable<T> collection) => AllItemsAreEqual(collection, null, out _);

        /// <summary>
        /// Determines whether all items in the specified collection are equal, and tries to get the unique item.
        /// </summary>
        /// <typeparam name="T">The type of items of <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The <see cref="IEnumerable{T}"/> to check equality between items.</param>
        /// <param name="comparer">
        /// An <see cref="IEqualityComparer{T}"/> to use to compare items, or
        /// <see langword="null"/> to use the default <see cref="IEqualityComparer{T}"/>.
        /// </param>
        /// <param name="value">
        /// When this method returns, <paramref name="value"/> contains the first item of <paramref name="collection"/>
        /// if all items are equal, or the default value for the <typeparamref name="T"/> type
        /// if <paramref name="collection"/> is empty or contains different items.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="collection"/> is not empty and all items are equal;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
        public static bool AllItemsAreEqual<T>(
            this IEnumerable<T> collection, IEqualityComparer<T>? comparer, [MaybeNullWhen(false)] out T value)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            comparer ??= EqualityComparer<T>.Default;
            value = default!;
            bool isFirst = true;

            foreach (var item in collection)
            {
                if (isFirst)
                {
                    value = item;
                    isFirst = false;
                }
                else if (!comparer.Equals(item, value))
                {
                    value = default;
                    return false;
                }
            }
            return !isFirst;
        }
    }
}
