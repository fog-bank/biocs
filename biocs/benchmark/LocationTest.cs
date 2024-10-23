using BenchmarkDotNet.Attributes;
using Biocs;

namespace Benchmark;

[MemoryDiagnoser]
//[ShortRunJob]
public class LocationTest
{
    [Params(10)] // 10, 20
    public int N;

    [Params(1000)] // 100, 1000, 10000
    public int Length;

    [Params(1)]
    public int Seed;

    private SequenceRange[]? ranges;
    private SequenceRange[]? ranges2;
    private SequenceRange exclude;

    [Benchmark]
    public LinkedListLocation LinkedList_Random()
    {
        var loc = new LinkedListLocation();

        for (int i = 0; i < ranges!.Length; i++)
        {
            var range = ranges[i];

            if (loc.Length <= Length / 3)
                loc.UnionWith(range);
            else if (loc.Length >= Length * 2 / 3)
            {
                loc.ExceptWith(range);
            }
            else
                loc.SymmetricExceptWith(range);
        }
        return loc;
    }

    [Benchmark]
    public DequeLocation Deque_Random()
    {
        var loc = new DequeLocation();

        for (int i = 0; i < ranges!.Length; i++)
        {
            var range = ranges![i];

            if (loc.Length <= Length / 3)
                loc.UnionWith(range);
            else if (loc.Length >= Length * 2 / 3)
            {
                loc.ExceptWith(range);
            }
            else
                loc.SymmetricExceptWith(range);
        }
        return loc;
    }

    [Benchmark]
    public ListLocation List_Random()
    {
        var loc = new ListLocation();

        for (int i = 0; i < ranges!.Length; i++)
        {
            var range = ranges![i];

            if (loc.Length <= Length / 3)
                loc.UnionWith(range);
            else if (loc.Length >= Length * 2 / 3)
            {
                loc.ExceptWith(range);
            }
            else
                loc.SymmetricExceptWith(range);
        }
        return loc;
    }

    [Benchmark]
    public LinkedListLocation LinkedList_FromBack()
    {
        var loc = new LinkedListLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.ExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public DequeLocation Deque_FromBack()
    {
        var loc = new DequeLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.ExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public ListLocation List_FromBack()
    {
        var loc = new ListLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.ExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public LinkedListLocation LinkedList_FromFront()
    {
        var loc = new LinkedListLocation();

        for (int i = ranges2!.Length - 1; i >= 0; i--)
            loc.UnionWith(ranges2[i]);

        loc.SymmetricExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public DequeLocation Deque_FromFront()
    {
        var loc = new DequeLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.SymmetricExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public ListLocation List_FromFront()
    {
        var loc = new ListLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.SymmetricExceptWith(exclude);
        return loc;
    }

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(Seed);
        ranges = new SequenceRange[N];

        for (int i = 0; i < ranges.Length; i++)
        {
            int pos1 = rnd.Next(1, Length);
            int pos2 = rnd.Next(1, Length);
            var range = new SequenceRange(Math.Min(pos1, pos2), Math.Max(pos1, pos2));
            ranges[i] = range;
        }

        ranges2 = new SequenceRange[N - 1];
        int step = Math.Max(Length / N / 2, 2);

        for (int i = 0; i < ranges2.Length; i++)
        {
            int from = Length - step * (2 * i + 1);
            if (from <= 0)
            {
                Array.Resize(ref ranges2, i);
                break;
            }
            ranges2[i] = new(from, Length - step * 2 * i);
        }
        exclude = new(1, Length);
    }
}
