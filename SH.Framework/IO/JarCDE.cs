using SH.Framework.Extensions;
using System;
using System.IO;

namespace SH.Framework.IO;

internal sealed class JarCDE
{
    //////////////////////////////////////////////////
    // Offset   Size   Field
    //   0       4     Signature (0x02014B50)
    //   4       2     Version made by
    //   6       2     Version needed
    //   8       2     General purpose bit flag
    //   10      2     Compression method
    //   12      2     Mod time
    //   14      2     Mod date
    //   16      4     CRC-32
    //   20      4     Compressed size
    //   24      4     Uncompressed size
    //   28      2     File name length
    //   30      2     Extra field length
    //   32      2     File comment length
    //   34      2     Disk number start
    //   36      2     Internal file attributes
    //   38      4     External file attributes
    //   42      4     Relative offset of local header
    //   46      n     File Name
    //   46+n    m     Extra Field
    //   46+n+m  k     File Comment
    //////////////////////////////////////////////////

    public JarCDE(JarLFE lfe)
    {
        LFE = lfe ?? throw new ArgumentNullException(nameof(lfe));
        ExternalFileAttributes =
            Filename.EndsWith('/') && UncompressedSize == 0 ? ExternalFileAttributes_Directory :
            Filename.EndsWith(".class", StringComparison.OrdinalIgnoreCase) ? ExternalFileAttributes_Class :
            ExternalFileAttributes_RegularFile;
    }

    private const uint Signature = 0x02014B50;
    private const ushort VersionMadeBy = 10;
    private const ushort VersionNeeded = 20;
    private const ushort GeneralPurposeBitFlag = 0x0800; // UTF-8 filename
    private const ushort CompressionMethod = 0;
    private const uint ExternalFileAttributes_Class = 0x81A40000; // ends with .class
    private const uint ExternalFileAttributes_ExecutableOrShell = 0x81ED0000; // executable or shell scripts
    private const uint ExternalFileAttributes_RegularFile = 0x81A40000; // regular files
    private const uint ExternalFileAttributes_Directory = 0x41ED0010; // or just 0x41ED0000 (without DOS directory flag)

    private readonly JarLFE LFE;
    private string Filename => LFE.Filename;
    private byte[] FilenameBytes => LFE.FilenameBytes;
    private uint Crc32 => LFE.Crc32;
    private ushort LastModDate => LFE.LastModDate;
    private ushort LastModTime => LFE.LastModTime;
    private uint CompressedSize => LFE.CompressedSize;
    private uint UncompressedSize => LFE.UncompressedSize;
    internal uint ExternalFileAttributes { get; set; }
    private uint LocalHeaderOffset => LFE.Offset;

    internal int TotalLength => 46 + FilenameBytes.Length; // + ExtraField.Length + FileComment.Length

    internal void Write(BinaryWriter bw)
    {
        bw.WriteUInt32LE(Signature); // CentralDirSignature
        bw.WriteUInt16LE(VersionMadeBy); // VersionMadeBy
        bw.WriteUInt16LE(VersionNeeded); // VersionNeeded
        bw.WriteUInt16LE(GeneralPurposeBitFlag); // GeneralPurposeBitFlag
        bw.WriteUInt16LE(CompressionMethod); // CompressionMethod
        bw.WriteUInt16LE(LastModTime); // LastModTime
        bw.WriteUInt16LE(LastModDate); // LastModDate
        bw.WriteUInt32LE(Crc32); // Crc32
        bw.WriteUInt32LE(CompressedSize); // CompressedSize
        bw.WriteUInt32LE(UncompressedSize); // UncompressedSize
        bw.WriteUInt16LE((ushort)FilenameBytes.Length); // FileNameLength
        bw.WriteUInt16LE(0); // ExtraFieldLength
        bw.WriteUInt16LE(0); // FileCommentLength
        bw.WriteUInt16LE(0); // DiskNumberStart
        bw.WriteUInt16LE(0); // InternalFileAttributes
        bw.WriteUInt32LE(ExternalFileAttributes); // ExternalFileAttributes
        bw.WriteUInt32LE(LocalHeaderOffset); // LocalHeaderOffset
        bw.Write(FilenameBytes); // FileName
        // ExtraField
        // FileComment
    }
}
