using BenchmarkDotNet.Attributes;
using Biocs;

namespace Benchmark;

[MemoryDiagnoser]
//[ShortRunJob]
public class LocationTest
{
    [Params(10, 20)]
    public int N;

    [Params(100, 1000, 10000)]
    public int GenomeLength;

    private SequenceRange[]? ranges;

    [Benchmark]
    public Location LinkedList()
    {
        var loc = new Location();

        for (int i = 0; i < N; i++)
        {
            var range = ranges![i];

            if (loc.Length <= GenomeLength / 3)
                loc.UnionWith(range);
            else if (loc.Length >= GenomeLength * 2 / 3)
            {
                loc.ExceptWith(range);
            }
            else
                loc.SymmetricExceptWith(range);
        }
        return loc;
    }

    [Benchmark]
    public DequeLocation Deque()
    {
        var loc = new DequeLocation();

        for (int i = 0; i < N; i++)
        {
            var range = ranges![i];

            if (loc.Length <= GenomeLength / 3)
                loc.UnionWith(range);
            else if (loc.Length >= GenomeLength * 2 / 3)
            {
                loc.ExceptWith(range);
            }
            else
                loc.SymmetricExceptWith(range);
        }
        return loc;
    }

    [Benchmark]
    public ListLocation List()
    {
        var loc = new ListLocation();

        for (int i = 0; i < N; i++)
        {
            var range = ranges![i];

            if (loc.Length <= GenomeLength / 3)
                loc.UnionWith(range);
            else if (loc.Length >= GenomeLength * 2 / 3)
            {
                loc.ExceptWith(range);
            }
            else
                loc.SymmetricExceptWith(range);
        }
        return loc;
    }

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random();
        ranges = new SequenceRange[N];

        for (int i = 0; i < ranges.Length; i++)
        {
            int pos1 = rnd.Next(1, GenomeLength);
            int pos2 = rnd.Next(1, GenomeLength);
            var range = new SequenceRange(Math.Min(pos1, pos2), Math.Max(pos1, pos2));
            ranges[i] = range;
        }
    }
}
