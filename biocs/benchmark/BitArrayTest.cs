using System.Buffers;
using System.Collections;
using BenchmarkDotNet.Order;

namespace Benchmark;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.Method)]
public class BitArrayTest
{
    [Params(44, 1000, Priority = -1)]
    public int N;

    [ParamsSource(nameof(Comparers))]
    public IEqualityComparer<BitArray> Comparer = default!;
    public static IEnumerable<IEqualityComparer<BitArray>> Comparers
        => [new GetComparer(), new GetComparer2(), new XorComparer(), new XorComparer2()];

    private HashSet<BitArray> hashSet = default!;
    private BitArray[] self = default!;
    private BitArray[] random = default!;

    [Benchmark]
    public int Self() => Contains(Data.Self);

    [Benchmark]
    public int Random() => Contains(Data.Random);

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random();
        hashSet = new(N - 3, Comparer);

        self = new BitArray[N - 3];
        for (int i = 0; i < self.Length; i++)
        {
            self[i] = new BitArray(N);
            SetBits(self[i], rnd);

            hashSet.Add(new(self[i]));
        }

        random = new BitArray[N - 3];
        for (int i = 0; i < random.Length; i++)
        {
            random[i] = new BitArray(N);
            SetBits(random[i], rnd);
        }
    }

    private static void SetBits(BitArray array, Random rnd)
    {
        int on = rnd.Next(2, array.Length - 2);

        for (int i = 0; i < on; i++)
        {
            int pos = rnd.Next(0, array.Length);
            array.Set(pos, true);
        }
    }

    internal static bool ByGet(BitArray? x, BitArray? y)
    {
        if (x == null || y == null)
            return x == y;

        if (x.Length != y.Length)
            return false;

        bool xor = false;
        for (int i = 0; i < x.Length; i++)
        {
            bool match = x.Get(i) != y.Get(i);
            if (i == 0)
                xor = match;
            else if (match != xor)
                return false;
        }
        return true;
    }

    internal static bool ByXor(BitArray? x, BitArray? y)
    {
        if (x == null || y == null)
            return x == y;

        if (x.Length != y.Length)
            return false;

        var comp = new BitArray(x);
        comp.Xor(y);
        return !comp.HasAnySet();
    }

    public enum Data
    {
        Self,
        Random
    }

    private BitArray[] GetData(Data type) => type switch
    {
        Data.Self => self,
        Data.Random => random,
        _ => null!
    };

    private int Contains(Data type)
    {
        int total = 0;
        foreach (var array in GetData(type))
        {
            if (hashSet.Contains(array))
                total++;
        }
        return total;
    }
}

file abstract class BitArrayComparer : EqualityComparer<BitArray>
{
    public sealed override int GetHashCode(BitArray obj)
    {
        var bytes = new byte[(obj.Length + 7) / 8];
        obj.CopyTo(bytes, 0);

        var hash = new HashCode();
        hash.AddBytes(bytes);
        return hash.ToHashCode();
    }
}

file abstract class BitArrayComparer2 : EqualityComparer<BitArray>
{
    public sealed override int GetHashCode(BitArray obj)
    {
        int length = (obj.Length + 7) / 8;
        var bytes = ArrayPool<byte>.Shared.Rent(length);
        obj.CopyTo(bytes, 0);

        var hash = new HashCode();
        hash.AddBytes(bytes.AsSpan(..length));
        ArrayPool<byte>.Shared.Return(bytes);
        return hash.ToHashCode();
    }
}

file sealed class GetComparer : BitArrayComparer
{
    public sealed override bool Equals(BitArray? x, BitArray? y)
        => BitArrayTest.ByGet(x, y);

    public sealed override string ToString() => "Get";
}

file sealed class XorComparer : BitArrayComparer
{
    public sealed override bool Equals(BitArray? x, BitArray? y)
        => BitArrayTest.ByXor(x, y);

    public sealed override string ToString() => "Xor";
}

file sealed class GetComparer2 : BitArrayComparer2
{
    public sealed override bool Equals(BitArray? x, BitArray? y)
        => BitArrayTest.ByGet(x, y);

    public sealed override string ToString() => "Get_Rent";
}

file sealed class XorComparer2 : BitArrayComparer2
{
    public sealed override bool Equals(BitArray? x, BitArray? y)
        => BitArrayTest.ByXor(x, y);

    public sealed override string ToString() => "Xor_Rent";
}
