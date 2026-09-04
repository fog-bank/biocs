using Biocs;

namespace Benchmark;

[MemoryDiagnoser]
public class LocationTest
{
    [Params(10, 100)]
    public int N;

    //[Params(1000, 10000)]
    public int Length = 1000;

    //[Params(1)]
    public int Seed = 1;

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
    public LinkedListLocation LinkedList_AddFirst()
    {
        var loc = new LinkedListLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.ExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public DequeLocation Deque_AddFirst()
    {
        var loc = new DequeLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.ExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public ListLocation List_AddFirst()
    {
        var loc = new ListLocation();

        for (int i = 0; i < ranges2!.Length; i++)
            loc.UnionWith(ranges2[i]);

        loc.ExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public LinkedListLocation LinkedList_AddLast()
    {
        var loc = new LinkedListLocation();

        for (int i = ranges2!.Length - 1; i >= 0; i--)
            loc.UnionWith(ranges2[i]);

        loc.SymmetricExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public DequeLocation Deque_AddLast()
    {
        var loc = new DequeLocation();

        for (int i = ranges2!.Length - 1; i >= 0; i--)
            loc.UnionWith(ranges2[i]);

        loc.SymmetricExceptWith(exclude);
        return loc;
    }

    [Benchmark]
    public ListLocation List_AddLast()
    {
        var loc = new ListLocation();

        for (int i = ranges2!.Length - 1; i >= 0; i--)
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
