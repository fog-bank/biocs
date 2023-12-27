using System.Reflection;
using System.Reflection.Emit;

namespace Biocs.TestTools;

/// <summary>
/// Describes a MSIL instruction and its operand.
/// </summary>
internal sealed class Instruction(OpCode opCode, int operand = 0)
{
    private static readonly Dictionary<int, OpCode> codeMap = GetOpCodes();

    public int Operand { get; } = operand;

    public bool IsCallMethod => opCode.OperandType == OperandType.InlineMethod;

    public bool IsLoadString => opCode.OperandType == OperandType.InlineString;

    public sealed override string? ToString() => opCode.ToString();

    /// <summary>
    /// Reads IL and gets <see cref="OpCode"/> and its operand (32-bit only).
    /// </summary>
    public static IEnumerable<Instruction?> ReadIL(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();

        if (il != null)
        {
            for (int pos = 0; pos < il.Length;)
            {
                int codeValue = il[pos++];

                yield return GetInstruction(codeValue, il, ref pos);
            }
        }
    }

    private static Instruction? GetInstruction(int codeValue, byte[] il, ref int pos)
    {
        // 2 bytes immediate operand value
        if (codeValue == 0xfe)
            codeValue = 0xfe00 + il[pos++];

        var opCode = codeMap[codeValue];

        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                return new Instruction(opCode);

            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                //byte op8 = il[pos];
                pos++;
                return new Instruction(opCode);

            case OperandType.InlineVar:
                //short op16 = BitConverter.ToInt16(il, pos);
                pos += 2;
                return new Instruction(opCode);

            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineI:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineSwitch:
            case OperandType.InlineTok:
            case OperandType.InlineType:
                int op32 = BitConverter.ToInt32(il, pos);
                pos += 4;

                if (opCode.OperandType == OperandType.InlineSwitch)
                    pos += 4 * op32;

                return new Instruction(opCode, op32);

            case OperandType.InlineI8:
                //long op64 = BitConverter.ToInt64(il, pos);
                pos += 8;
                return new Instruction(opCode);

            case OperandType.ShortInlineR:
                //float op32r = BitConverter.ToSingle(il, pos);
                pos += 4;
                return new Instruction(opCode);

            case OperandType.InlineR:
                //double op64r = BitConverter.ToDouble(il, pos);
                pos += 8;
                return new Instruction(opCode);
        }
        return null;
    }

    /// <summary>
    /// Creates a dictionary with the immediate operand value as key and the <see cref="OpCode"/> as value.
    /// </summary>
    private static Dictionary<int, OpCode> GetOpCodes()
    {
        var fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var map = new Dictionary<int, OpCode>(fields.Length);

        foreach (var field in fields)
        {
            var opCode = (OpCode)field.GetValue(null);
            map[opCode.Value & 0xffff] = opCode;
        }
        return map;
    }
}
