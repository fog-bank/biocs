namespace Biocs.Numerics;

/// <summary>
/// Provides static methods for <see cref="double"/> values.
/// </summary>
public static class DoubleTools
{
    /// <summary>
    /// Gets the minimum positive number x, such that 1.0 + x != 1.0.
    /// </summary>
    public const double Epsilon = 2.2204460492503131e-16;

    /// <summary>
    /// Determines if two <see cref="double"/> values are close to each other.
    /// </summary>
    /// <param name="one">The first value to compare.</param>
    /// <param name="other">The second value to compare.</param>
    /// <returns><see langword="true"/> if the values are close, <see langword="false"/> if they are not.</returns>
    public static bool AreClose(double one, double other)
    {
        if (one == other)
            return true;

        return Math.Abs(one - other) < (Math.Abs(one) + Math.Abs(other) + 10) * Epsilon;
    }

    /// <summary>
    /// Computes the sum of a sequence of <see cref="double"/> values using the second-order iterative Kahan–Babuška algorithm.
    /// </summary>
    /// <param name="values">A sequence of <see cref="double"/> values to calculate the sum of.</param>
    /// <returns>The sum of the values in the sequence.</returns>
    public static double SumKahan(IEnumerable<double> values)
    {
        var sum = new KahanSummation();
        sum.AddEnumerable(values);
        return sum.Result;
    }

    /// <summary>
    /// Computes the sum of a array of <see cref="double"/> values using the second-order iterative Kahan–Babuška algorithm.
    /// </summary>
    /// <param name="array">A array of <see cref="double"/> values to calculate the sum of.</param>
    /// <returns>The sum of the values in the array.</returns>
    public static double SumKahan(double[] array)
    {
        var sum = new KahanSummation();
        sum.AddSpan(array);
        return sum.Result;
    }

    /// <summary>
    /// Computes the sum of a span of <see cref="double"/> values using the second-order iterative Kahan–Babuška algorithm.
    /// </summary>
    /// <param name="span">A span of <see cref="double"/> values to calculate the sum of.</param>
    /// <returns>The sum of the values in the span.</returns>
    public static double SumKahan(ReadOnlySpan<double> span)
    {
        var sum = new KahanSummation();
        sum.AddSpan(span);
        return sum.Result;
    }
}

internal struct KahanSummation
{
    private double sum;
    private double cs;
    private double ccs;

    public readonly double Result => sum + cs + ccs;

    public void Add(double value)
    {
        double t = sum + value;
        double c = Math.Abs(sum) >= Math.Abs(value) ? sum - t + value : value - t + sum;
        sum = t;

        t = cs + c;
        double cc = Math.Abs(cs) >= Math.Abs(c) ? cs - t + c : c - t + cs;
        cs = t;
        ccs += cc;
    }

    public void AddEnumerable(IEnumerable<double> values)
    {
        foreach (double value in values)
            Add(value);
    }

    public void AddSpan(ReadOnlySpan<double> span)
    {
        foreach (double value in span)
            Add(value);
    }
}
