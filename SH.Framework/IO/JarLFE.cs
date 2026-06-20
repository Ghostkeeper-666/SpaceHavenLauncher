using SH.Framework.Extensions;
using SH.Framework.FastZip;
using System;
using System.IO;

namespace SH.Framework.IO;

internal sealed class JarLFE
{
    ////////////////////////////////////////////////////
    // Offset   Size   Field
    //   0       4     Signature (0x04034B50)
    //   4       2     Version Needed
    //   6       2     General Purpose Bit Flag
    //   8       2     Compression Method
    //   10      2     Mod Time
    //   12      2     Mod Date
    //   14      4     CRC-32
    //   18      4     Compressed Size
    //   22      4     Uncompressed Size
    //   26      2     File Name Length (n)
    //   28      2     Extra Field Length (m)
    //   30      n     File Name
    //   30+n    m     Extra Field
    //   30+n+m  c     File Data (Compressed Size bytes)
    ////////////////////////////////////////////////////

    private const uint Signature = 0x04034B50;
    private const ushort VersionNeeded = 10;
    private const ushort GeneralPurposeBitFlag = 0x0800; // UTF-8 filename
    private const ushort CompressionMethod = 0;

    internal string Filename { get; set; }
    internal byte[] FilenameBytes { get; set; }

    internal byte[] Data { get; set; }
    internal uint Crc32 { get; set; }

    internal uint CompressedSize { get; set; }
    internal uint UncompressedSize { get; set; }

    internal DateTime LastModified
    {
        get => JarDateTime.ToDateTime(LastModDate, LastModTime);
        init => JarDateTime.FromDateTime(value, out LastModDate, out LastModTime);
    }
    internal readonly ushort LastModDate;
    internal readonly ushort LastModTime;

    internal uint Offset;

    internal int TotalLength => 30 + FilenameBytes.Length + Data.Length;

    internal void Write(BinaryWriter bw)
    {
        BinaryX.WriteUInt32LE(bw, Signature);
        BinaryX.WriteUInt16LE(bw, VersionNeeded);
        BinaryX.WriteUInt16LE(bw, GeneralPurposeBitFlag);
        BinaryX.WriteUInt16LE(bw, CompressionMethod);
        BinaryX.WriteUInt16LE(bw, LastModTime);
        BinaryX.WriteUInt16LE(bw, LastModDate);
        BinaryX.WriteUInt32LE(bw, Crc32);
        BinaryX.WriteUInt32LE(bw, CompressedSize);
        BinaryX.WriteUInt32LE(bw, UncompressedSize);
        BinaryX.WriteUInt16LE(bw, (ushort)FilenameBytes.Length); // FileNameLength
        BinaryX.WriteUInt16LE(bw, 0); // ExtraFieldLength
        bw.Write(FilenameBytes); // FileName
        // ExtraField
        if (Data.Length > 0) bw.Write(Data); // FileData
    }
}
