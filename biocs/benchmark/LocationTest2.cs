using Biocs;
using Biocs.Text;

namespace Benchmark;

[MemoryDiagnoser]
public class LocationTest2
{
    private List<SequenceRange> ranges = default!;

    [Benchmark]
    public ListLocation List()
    {
        var loc = new ListLocation();

        foreach (var range in ranges)
            loc.UnionWith(range);

        return loc;
    }

    [Benchmark]
    public LinkedListLocation LinkedList()
    {
        var loc = new LinkedListLocation();

        foreach (var range in ranges)
            loc.UnionWith(range);

        return loc;
    }

    [Benchmark]
    public DequeLocation Deque()
    {
        var loc = new DequeLocation();

        foreach (var range in ranges)
            loc.UnionWith(range);

        return loc;
    }

    [GlobalSetup]
    public void Setup()
    {
        ranges = new(228508);

        foreach (string line in File.ReadLines(Path.Combine("Deployments", "coordinates.txt")))
        {
            var (start, end) = line.AsTsv();
            ranges.Add(new(int.Parse(start), int.Parse(end)));
        }
    }
}
