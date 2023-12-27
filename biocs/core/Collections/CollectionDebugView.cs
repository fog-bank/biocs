using System.Diagnostics;
namespace Biocs.Collections;

internal sealed class CollectionDebugView<T>(ICollection<T>? collection)
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            if (collection == null)
                return [];

            var items = new T[collection.Count];
            collection.CopyTo(items, 0);
            return items;
        }
    }
}
