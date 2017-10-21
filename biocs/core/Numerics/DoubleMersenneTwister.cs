using System;
using System.Runtime.InteropServices;

namespace Biocs.Numerics
{
    /// <summary>
    /// Represents double-precision Mersenne Twister pseudorandom number generator based on IEEE 754 format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For details about Mersenne Twister, see http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/SFMT/.
    /// </para>
    /// <para>Currently, the environment where the architecture is big-endian is not supported.</para>
    /// </remarks>
    public class DoubleMersenneTwister
    {
        private const int Exp = 19937;
        private const int N = (Exp - 128) / 104 + 1;
        //private const int N32 = N * 4;
        private const int N64 = N * 2;

        private Union128[] status = new Union128[N + 1];
        private int index = N64;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleMersenneTwister"/> class, 
        /// using a time-dependent default seed value.
        /// </summary>
        public DoubleMersenneTwister()
            : this(Environment.TickCount)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleMersenneTwister"/> class, using the specified seed value.
        /// </summary>
        /// <param name="seed">A 32-bit integer used as the seed.</param>
        public DoubleMersenneTwister(int seed)
        {
            unchecked
            {
                status[0].u0 = (uint)seed;
                uint pos = 1;

                for (int i = 0; i < status.Length; i++)
                {
                    if (i != 0)
                        status[i].u0 = 1812433253u * (status[i - 1].u3 ^ (status[i - 1].u3 >> 30)) + pos++;

                    status[i].u1 = 1812433253u * (status[i].u0 ^ (status[i].u0 >> 30)) + pos++;
                    status[i].u2 = 1812433253u * (status[i].u1 ^ (status[i].u1 >> 30)) + pos++;
                    status[i].u3 = 1812433253u * (status[i].u2 ^ (status[i].u2 >> 30)) + pos++;
                }
            }
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
            if (seeds == null)
                throw new ArgumentNullException(nameof(seeds));

            const int size = (N + 1) * 4;
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

            const int lag = 11;
            const int mid = ((N + 1) * 4 - lag) / 2;

            for (int i = 0; i < status.Length; i++)
            {
                status[i].ul0 = 0x8b8b8b8b8b8b8b8bu;
                status[i].ul1 = 0x8b8b8b8b8b8b8b8bu;
            }
            int count = Math.Max(seeds.Length + 1, size);
            uint r = InitValue1(RefToUInt32(0) ^ RefToUInt32(mid % size) ^ RefToUInt32((size - 1) % size));

            unchecked
            {
                RefToUInt32(mid % size) += r;
                r += (uint)seeds.Length;
                RefToUInt32((mid + lag) % size) += r;
                RefToUInt32(0) = r;
                count--;

                int i = 1, j = 0;
                for (; j < count && j < seeds.Length; j++)
                {
                    r = InitValue1(RefToUInt32(i) ^ RefToUInt32((i + mid) % size) ^ RefToUInt32((i + size - 1) % size));
                    RefToUInt32((i + mid) % size) += r;
                    r += (uint)seeds[j] + (uint)i;
                    RefToUInt32((i + mid + lag) % size) += r;
                    RefToUInt32(i) = r;
                    i = (i + 1) % size;
                }

                for (; j < count; j++)
                {
                    r = InitValue1(RefToUInt32(i) ^ RefToUInt32((i + mid) % size) ^ RefToUInt32((i + size - 1) % size));
                    RefToUInt32((i + mid) % size) += r;
                    r += (uint)i;
                    RefToUInt32((i + mid + lag) % size) += r;
                    RefToUInt32(i) = r;
                    i = (i + 1) % size;
                }

                for (j = 0; j < size; j++)
                {
                    r = InitValue2(RefToUInt32(i) + RefToUInt32((i + mid) % size) + RefToUInt32((i + size - 1) % size));
                    RefToUInt32((i + mid) % size) ^= r;
                    r -= (uint)i;
                    RefToUInt32((i + mid + lag) % size) ^= r;
                    RefToUInt32(i) = r;
                    i = (i + 1) % size;
                }
            }
            InitialMask();
            CertificatePeriod();
        }

        /// <summary>
        /// Returns a double-precision pseudorandom number that distributes uniformly in the range [0, 1).
        /// </summary>
        /// <returns>A random floating-point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public double Next() => Sample() - 1;

        /// <summary>
        /// Returns a double-precision pseudorandom number that distributes uniformly in the range (0, 1).
        /// </summary>
        /// <returns>A random floating-point number that is greater than 0.0, and less than 1.0.</returns>
        public double NextOpen()
        {
            var r = new Union128() { d0 = Sample() };
            r.ul0 |= 1;

            return r.d0 - 1;
        }

        // Initializes the internal state array to fit the IEEE 754 format.
        private void InitialMask()
        {
            const ulong LowMask = 0x000fffffffffffff;
            const ulong HighConst = 0x3ff0000000000000;

            for (int i = 0; i < status.Length - 1; i++)
            {
                status[i].ul0 = (status[i].ul0 & LowMask) | HighConst;
                status[i].ul1 = (status[i].ul1 & LowMask) | HighConst;
            }
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

            Recurse(ref status[0], ref status[0], ref status[Pos1], ref lung);

            int i = 1;
            for (; i < N - Pos1; i++)
                Recurse(ref status[i], ref status[i], ref status[i + Pos1], ref lung);

            for (; i < N; i++)
                Recurse(ref status[i], ref status[i], ref status[i + Pos1 - N], ref lung);

            status[N] = lung;
        }

        private double Sample()
        {
            if (index >= N64)
            {
                GenerateRand();
                index = 0;
            }
            return index % 2 == 0 ? status[index++ / 2].d0 : status[index++ / 2].d1;
        }

        private static uint InitValue1(uint x) => unchecked((x ^ (x >> 27)) * 1664525u);

        private static uint InitValue2(uint x) => unchecked((x ^ (x >> 27)) * 1566083941u);

        private ref uint RefToUInt32(int index)
        {
            switch (index % 4)
            {
                case 0:
                    return ref status[index / 4].u0;

                case 1:
                    return ref status[index / 4].u1;

                case 2:
                    return ref status[index / 4].u2;

                case 3:
                    return ref status[index / 4].u3;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        // Represents the recursion formula.
        private static void Recurse(ref Union128 r, ref Union128 a, ref Union128 b, ref Union128 lung)
        {
            const int SR = 12;

            // Constants for Exp of 19937
            const int SL1 = 19;
            const ulong Msk1 = 0x000ffafffffffb3f;
            const ulong Msk2 = 0x000ffdfffc90fffd;

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

    [StructLayout(LayoutKind.Explicit)]
    internal struct Union128
    {
        [FieldOffset(0)]
        public double d0;

        [FieldOffset(8)]
        public double d1;

        [FieldOffset(0)]
        public uint u0;

        [FieldOffset(4)]
        public uint u1;

        [FieldOffset(8)]
        public uint u2;

        [FieldOffset(12)]
        public uint u3;

        [FieldOffset(0)]
        public ulong ul0;

        [FieldOffset(8)]
        public ulong ul1;

        public override string ToString() => $"0x{ul1:x}{ul0:x}";
    }
}
