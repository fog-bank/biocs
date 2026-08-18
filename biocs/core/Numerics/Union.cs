using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Biocs.Numerics;

[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("Style", "IDE1006:Naming Styles")]
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

    [FieldOffset(0)]
    public Vector128<ulong> si;

    [ExcludeFromCodeCoverage]
    public override readonly string ToString() => $"0x{ul1:x}{ul0:x}";
}

[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("Style", "IDE1006:Naming Styles")]
internal struct Union64
{
    [FieldOffset(0)]
    public double d;

    [FieldOffset(0)]
    public ulong ul;

    [ExcludeFromCodeCoverage]
    public override readonly string ToString() => $"0x{ul:x}";
}
