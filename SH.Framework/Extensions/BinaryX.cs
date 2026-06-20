using System.IO;

namespace SH.Framework.Extensions;

public static class BinaryX
{
    public static ushort ReadUInt16LE(this BinaryReader br) =>
        (ushort)(br.ReadByte() | (br.ReadByte() << 8));

    public static uint ReadUInt32LE(this BinaryReader br) =>
        (uint)(br.ReadByte()
        | (br.ReadByte() << 8)
        | (br.ReadByte() << 16)
        | (br.ReadByte() << 24));

    public static void WriteUInt16LE(this BinaryWriter bw, ushort value)
    {
        bw.Write((byte)(value & 0xFF));
        bw.Write((byte)((value >> 8) & 0xFF));
    }

    public static void WriteUInt32LE(this BinaryWriter bw, uint value)
    {
        bw.Write((byte)(value & 0xFF));
        bw.Write((byte)((value >> 8) & 0xFF));
        bw.Write((byte)((value >> 16) & 0xFF));
        bw.Write((byte)((value >> 24) & 0xFF));
    }
}