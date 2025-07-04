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
    /// Initializes a new instance of the <see cref="DiscreteDistribution"/> class that the number of elements is 1.
    /// </summary>
    /// <remarks>
    /// <see cref="Length"/> of this instance is 1 and <see cref="Next(double)"/> returns always 0
    /// unless <see cref="Reset"/> with other weights.
    /// </remarks>
    public DiscreteDistribution() : this(default)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteDistribution"/> class.
    /// </summary>
    /// <param name="weights">A array that contains the weight or probability of each element.</param>
    /// <exception cref="ArgumentException">
    /// <para><paramref name="weights"/> contains a negative or non-finite value.</para> -or-
    /// <para>The sum of <paramref name="weights"/> is equal to or less than 0.</para>
    /// </exception>
    /// <remarks>If the length of <paramref name="weights"/> is 0, this is equivalent to the default constructor.</remarks>
    public DiscreteDistribution(ReadOnlySpan<double> weights)
    {
        if (weights.Length == 0)
            weights = [1.0];

        cutoff = new double[weights.Length];
        alias = new int[weights.Length];
        Initialize(weights);
    }

    /// <summary>
    /// Gets the number of items in this distribution range.
    /// </summary>
    public int Length => cutoff.Length;

    /// <summary>
    /// Returns a non-negative random interger that is less than <see cref="Length"/>.
    /// </summary>
    /// <param name="randomNumber">A random floating-point number that distributes uniformly in the range [0, 1).</param>
    /// <returns>An integer chosen based on the current distribution.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="randomNumber"/> is less than 0.</para> -or- 
    /// <para><paramref name="randomNumber"/> is equal to or greater than 1.</para>
    /// </exception>
    public int Next(double randomNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(randomNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(randomNumber, 1);

        double scaledProb = randomNumber * Length;
        int index = (int)Math.Truncate(scaledProb);
        double prob = scaledProb - index;
        return prob < cutoff[index] ? index : alias[index];
    }

    /// <summary>
    /// Returns a non-negative random interger that is less than <see cref="Length"/> using the specified pseudo-random number
    /// generator.
    /// </summary>
    /// <param name="random">A <see cref="Random"/> instance that generates double-precision floating point number.</param>
    /// <returns>An integer chosen based on the current distribution.</returns>
    public int Next(Random random) => Next(random.NextDouble());

    /// <summary>
    /// Resets the weight or probability of each element to new values.
    /// </summary>
    /// <param name="weights">A array that contains the weight or probability of each element.</param>
    /// <exception cref="ArgumentException">
    /// <para><paramref name="weights"/> contains a negative or non-finite value.</para> -or-
    /// <para>The sum of <paramref name="weights"/> is equal to or less than 0.</para>
    /// </exception>
    public void Reset(ReadOnlySpan<double> weights)
    {
        if (weights.Length == 0)
            weights = [1.0];

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

        double sum = DoubleTools.SumKahan(weights);

        if (!double.IsNormal(sum))
            ThrowHelper.ThrowArgument(null, nameof(weights));

        for (int i = 0; i < cutoff.Length; i++)
        {
            double scaledProb = cutoff[i] * cutoff.Length / sum;
            cutoff[i] = scaledProb;
            alias[i] = i;

            switch (scaledProb)
            {
                case < 0:
                    ThrowHelper.ThrowArgument(null, nameof(weights));
                    return;

                case < 1:
                    under.Push(i);
                    break;

                case > 1:
                    over.Push(i);
                    break;
            }
        }

        while (under.Count > 0)
        {
            int underIndex = under.Pop();

            if (over.Count == 0)
            {
                // Possible due to numerical instability.
                break;
            }

            int overIndex = over.Pop();
            double overCutoff = cutoff[underIndex] + cutoff[overIndex] - 1;
            cutoff[overIndex] = overCutoff;
            alias[underIndex] = overIndex;

            switch (overCutoff)
            {
                case < 1:
                    under.Push(overIndex);
                    break;

                case > 1:
                    over.Push(overIndex);
                    break;
            }
        }
    }
}
