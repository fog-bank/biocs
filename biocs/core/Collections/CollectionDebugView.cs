#if MIN_NETSTANDARD1_3
using System;
#endif
using System.Collections.Generic;
using System.Diagnostics;

namespace Biocs.Collections
{
    internal sealed class CollectionDebugView<T>
    {
        private readonly ICollection<T> coll;

        public CollectionDebugView(ICollection<T> collection) => coll = collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                if (coll != null)
                {
                    var items = new T[coll.Count];
                    coll.CopyTo(items, 0);
                    return items;
                }
                else
                {
#if MIN_NETSTANDARD1_3
                    return Array.Empty<T>();
#else
                    return new T[0];

#endif
                }
            }
        }
    }
}
