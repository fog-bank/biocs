using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Biocs.Numerics;

/// <summary>
/// Represents double-precision SIMD-oriented Fast Mersenne Twister pseudorandom number generator.
/// </summary>
/// <remarks>
/// <para>For details about Mersenne Twister, see http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/SFMT/.</para>
/// <para>Currently, a big-endian architecture is not supported.</para>
/// </remarks>
public class DoubleMersenneTwister
{
    private const int Exp = 19937;
    private const int N = (Exp - 128) / 104 + 1;
    private const int N64 = N * 2;

    private readonly Union128[] status = new Union128[N + 1];
    private int index = N64;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleMersenneTwister"/> class.
    /// </summary>
    public DoubleMersenneTwister() : this(unchecked((int)Random.Shared.NextInt64()))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleMersenneTwister"/> class, using the specified seed value.
    /// </summary>
    /// <param name="seed">A 32-bit integer used as the seed.</param>
    public DoubleMersenneTwister(int seed)
    {
        var span = MemoryMarshal.Cast<Union128, uint>(status.AsSpan());
        span[0] = unchecked((uint)seed);

        for (int i = 1; i < span.Length; i++)
            span[i] = unchecked(1812433253u * (span[i - 1] ^ (span[i - 1] >> 30)) + (uint)i);

        InitialMask();
        CertificatePeriod();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleMersenneTwister"/> class, using the specified seed array.
    /// </summary>
    /// <param name="seeds">An array of 32-bit integers used as the seed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="seeds"/> is <see langword="null"/>.</exception>
    public DoubleMersenneTwister(int[] seeds)
    {
        ArgumentNullException.ThrowIfNull(seeds);

        const int Size = (N + 1) * 4;
        const int Lag = 11;
        const int Mid = (Size - Lag) / 2;

        //int lag = 0;

        //if (size >= 623)
        //    lag = 11;
        //else if (size >= 68)
        //    lag = 7;
        //else if (size >= 39)
        //    lag = 5;
        //else
        //    lag = 3;

        //int mid = (size - lag) / 2;

        var span = MemoryMarshal.Cast<Union128, uint>(status.AsSpan());
        span.Fill(0x8b8b8b8bu);

        int count = Math.Max(seeds.Length + 1, Size);
        uint r = InitValue1(span[0] ^ span[Mid % Size] ^ span[(Size - 1) % Size]);

        unchecked
        {
            span[Mid % Size] += r;
            r += (uint)seeds.Length;
            span[(Mid + Lag) % Size] += r;
            span[0] = r;
            count--;

            int i = 1, j = 0;
            for (; j < count && j < seeds.Length; j++)
            {
                r = InitValue1(span[i] ^ span[(i + Mid) % Size] ^ span[(i + Size - 1) % Size]);
                span[(i + Mid) % Size] += r;
                r += (uint)seeds[j] + (uint)i;
                span[(i + Mid + Lag) % Size] += r;
                span[i] = r;
                i = (i + 1) % Size;
            }

            for (; j < count; j++)
            {
                r = InitValue1(span[i] ^ span[(i + Mid) % Size] ^ span[(i + Size - 1) % Size]);
                span[(i + Mid) % Size] += r;
                r += (uint)i;
                span[(i + Mid + Lag) % Size] += r;
                span[i] = r;
                i = (i + 1) % Size;
            }

            for (j = 0; j < Size; j++)
            {
                r = InitValue2(span[i] + span[(i + Mid) % Size] + span[(i + Size - 1) % Size]);
                span[(i + Mid) % Size] ^= r;
                r -= (uint)i;
                span[(i + Mid + Lag) % Size] ^= r;
                span[i] = r;
                i = (i + 1) % Size;
            }
        }
        InitialMask();
        CertificatePeriod();
    }

    /// <summary>
    /// Returns a double-precision pseudorandom number that distributes uniformly in the range [0, 1).
    /// </summary>
    /// <returns>A random floating-point number that is greater than or equal to 0.0, and less than 1.0.</returns>
    public double NextDouble() => Sample() - 1;

    /// <summary>
    /// Returns a double-precision pseudorandom number that distributes uniformly in the range (0, 1).
    /// </summary>
    /// <returns>A random floating-point number that is greater than 0.0, and less than 1.0.</returns>
    public double NextDoubleOpen()
    {
        var r = new Union64() { d = Sample() };
        r.ul |= 1;

        return r.d - 1;
    }

    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    /// <returns>A 32-bit unsigned integer.</returns>
    public uint NextUInt32()
    {
        if (index >= N64)
        {
            GenerateRand();
            index = 0;
        }
        ulong ul = MemoryMarshal.Cast<Union128, ulong>(status.AsSpan())[index++];
        return unchecked((uint)ul);
    }

    // Initializes the internal state array to fit the IEEE 754 format.
    private void InitialMask()
    {
        const ulong LowMask = 0x000fffffffffffff;
        const ulong HighConst = 0x3ff0000000000000;

        var span = MemoryMarshal.Cast<Union128, ulong>(status.AsSpan(0..^1));

        for (int i = 0; i < span.Length; i++)
            span[i] = (span[i] & LowMask) | HighConst;
    }

    // Certifacates the period of 2 ^ Exp - 1.
    private void CertificatePeriod()
    {
        // Constants for Exp of 19937
        const ulong PCV1 = 0x3d84e1ac0dc82880;
        const ulong PCV2 = 0x0000000000000001;
        const ulong Fix1 = 0x90014964b32f4329;
        const ulong Fix2 = 0x3b8d12ac548a7c7a;

        ulong tmp0 = status[N].ul0 ^ Fix1;
        ulong tmp1 = status[N].ul1 ^ Fix2;
        ulong inner = tmp0 & PCV1;
        inner ^= tmp1 & PCV2;

        for (int i = 32; i > 0; i >>= 1)
            inner ^= inner >> i;

        inner &= 1;

        if (inner == 1)
            return;

        if ((PCV2 & 1) == 1)
            status[N].ul1 ^= 1;
        //else
        //{
        //    ulong work = 1;

        //    for (int j = 0; j < 64; j++)
        //    {
        //        if ((work & PCV2) != 0)
        //        {
        //            status[N].ul1 ^= work;
        //            return;
        //        }
        //        work <<= 1;
        //    }
        //    work = 1;

        //    for (int j = 0; j < 64; j++)
        //    {
        //        if ((work & PCV1) != 0)
        //        {
        //            status[N].ul0 ^= work;
        //            return;
        //        }
        //        work <<= 1;
        //    }
        //}
    }

    private void GenerateRand()
    {
        // Constants for Exp of 19937
        const int Pos1 = 117;

        var lung = status[N];

        Recurse(ref status[0], status[0], status[Pos1], ref lung);

        int i = 1;
        for (; i < N - Pos1; i++)
            Recurse(ref status[i], status[i], status[i + Pos1], ref lung);

        for (; i < N; i++)
            Recurse(ref status[i], status[i], status[i + Pos1 - N], ref lung);

        status[N] = lung;
    }

    private double Sample()
    {
        if (index >= N64)
        {
            GenerateRand();
            index = 0;
        }
        return MemoryMarshal.Cast<Union128, double>(status.AsSpan())[index++];
    }

    private static uint InitValue1(uint x) => unchecked((x ^ (x >> 27)) * 1664525u);

    private static uint InitValue2(uint x) => unchecked((x ^ (x >> 27)) * 1566083941u);

    // Represents the recursion formula.
    private static void Recurse(ref Union128 r, in Union128 a, in Union128 b, ref Union128 lung)
    {
        const int SR = 12;

        // Constants for Exp of 19937
        const int SL1 = 19;
        const ulong Msk1 = 0x000ffafffffffb3f;
        const ulong Msk2 = 0x000ffdfffc90fffd;

        if (Vector128.IsHardwareAccelerated)
        {
            var x = a.si;
            var z = x << SL1;
            var y = Vector128.Shuffle(lung.si.AsUInt32(), Vector128.Create(3u, 2u, 1u, 0u)).AsUInt64();
            z ^= b.si;
            y ^= z;

            var v = y >>> SR;
            var w = y & Vector128.Create(Msk1, Msk2);
            v ^= x;
            v ^= w;
            r.si = v;
            lung.si = y;
        }
        else
        {
            ulong t0 = a.ul0;
            ulong t1 = a.ul1;
            ulong l0 = lung.ul0;
            ulong l1 = lung.ul1;

            lung.ul0 = (t0 << SL1) ^ (l1 >> 32) ^ (l1 << 32) ^ b.ul0;
            lung.ul1 = (t1 << SL1) ^ (l0 >> 32) ^ (l0 << 32) ^ b.ul1;
            r.ul0 = (lung.ul0 >> SR) ^ (lung.ul0 & Msk1) ^ t0;
            r.ul1 = (lung.ul1 >> SR) ^ (lung.ul1 & Msk2) ^ t1;
        }
    }
}
