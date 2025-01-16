namespace Biocs.Numerics;

/// <summary>
/// Provides a sampling method from a discrete probability distribution.
/// </summary>
/// <remarks>This class implements Walker's alias method.</remarks>
public class DiscreteDistribution
{
    private double[] cutoff;
    private int[] alias;
    private readonly Stack<int> under = new();
    private readonly Stack<int> over = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteDistribution"/> class.
    /// </summary>
    public DiscreteDistribution() : this(default)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteDistribution"/> class.
    /// </summary>
    /// <param name="weights">A array that contains the weight or probability of each element.</param>
    public DiscreteDistribution(ReadOnlySpan<double> weights)
    {
        cutoff = new double[weights.Length];
        alias = new int[weights.Length];
        Initialize(weights);
    }

    /// <summary>
    /// Gets the number of items that this distribution represents.
    /// </summary>
    public int Length => cutoff.Length;

    /// <summary>
    /// Returns a non-negative random interger that is less than <see cref="Length"/>.
    /// </summary>
    /// <param name="randomNumber">A random floating-point number that distributes uniformly in the range [0, 1).</param>
    /// <returns>An index value (0-origin) chosen based on the current distribution.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="randomNumber"/> is less than 0.</para> -or- 
    /// <para><paramref name="randomNumber"/> is equal to or greater than 1.</para>
    /// </exception>
    public int NextIndex(double randomNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(randomNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(randomNumber, 1);

        double scaledProb = randomNumber * Length;
        int index = (int)Math.Truncate(scaledProb);
        double prob = scaledProb - index;
        return prob <= cutoff[index] ? index : alias[index];
    }

    /// <summary>
    /// Returns a non-negative random interger that is less than <see cref="Length"/> using the specified pseudo-random number
    /// generator.
    /// </summary>
    /// <param name="random">A <see cref="Random"/> instance that generates double-precision floating point number.</param>
    /// <returns>An index value (0-origin) chosen based on the current distribution.</returns>
    public int NextIndex(Random random) => NextIndex(random.NextDouble());

    /// <summary>
    /// Resets the weight or probability of each element to new value.
    /// </summary>
    /// <param name="weights">A array that contains the weight or probability of each element.</param>
    public void Reset(ReadOnlySpan<double> weights)
    {
        if (Length != weights.Length)
        {
            cutoff = new double[weights.Length];
            alias = new int[weights.Length];
        }
        Initialize(weights);
    }

    private void Initialize(ReadOnlySpan<double> weights)
    {
        weights.CopyTo(cutoff);
        under.Clear();
        over.Clear();

        double sum = cutoff.Sum();

        for (int i = 0; i < cutoff.Length; i++)
        {
            double scaledProb = cutoff[i] * cutoff.Length / sum;
            cutoff[i] = scaledProb;

            if (scaledProb < 1)
                under.Push(i);
            else if (scaledProb > 1)
                over.Push(i);

            if (double.IsNaN(scaledProb) || scaledProb < 0)
                ThrowHelper.ThrowArgument(null, nameof(weights[i]));
        }

        while (under.Count > 0)
        {
            int underIndex = under.Pop();

            if (over.Count == 0)
            {
                alias[underIndex] = underIndex;
                continue;
            }

            int overIndex = over.Peek();
            double overCutoff = cutoff[underIndex] + cutoff[overIndex] - 1;
            alias[underIndex] = overIndex;
            cutoff[overIndex] = overCutoff;

            if (overCutoff <= 1)
            {
                over.Pop();

                if (overCutoff < 1)
                    under.Push(overIndex);
            }
        }
    }
}
