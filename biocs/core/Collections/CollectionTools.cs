using System;
using System.Collections.Generic;

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
        public static bool AllItemsAreEqual<T>(this IEnumerable<T> collection, IEqualityComparer<T> comparer, out T value)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            value = default;

            using (var enumetator = collection.GetEnumerator())
            {
                if (!enumetator.MoveNext())
                    return false;

                var item = enumetator.Current;

                while (enumetator.MoveNext())
                {
                    if (!comparer.Equals(enumetator.Current, item))
                        return false;
                }
                value = item;
                return true;
            }
        }

        internal static T[] Empty<T>()
        {
#if MIN_NETSTANDARD1_3
            return Array.Empty<T>();
#else
            return new T[0];
#endif
        }
    }
}
