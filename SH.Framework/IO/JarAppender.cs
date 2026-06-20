using SH.Framework.Cryptography;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Framework.IO;

public sealed class JarAppender
{

    /// <summary>
    /// A simple class for adding uncompressed files to a JAR file as fast as possible.
    /// File sizes bigger than 2GB are not supported.
    /// </summary>
    public async Task<bool> AppendTo(string sourcePath, string targetPath, string baseDir, FileInfo[] newFiles, ILogger logger, ParallelOptions parallelOptions)
    {
        const string tooBig = "JAR file is too big";
        try
        {
            IOUtils.ThrowIfFileNotExists(sourcePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
            IOUtils.ThrowIfDirectoryNotExists(baseDir);
            ArgumentNullException.ThrowIfNull(newFiles, nameof(newFiles));
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newFiles.Length, 0, nameof(newFiles));
            FileInfo sourceFI = new(sourcePath);
            long sourceFileSize = sourceFI.Length;
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sourceFI.Length, int.MaxValue, nameof(sourceFileSize));

            // Read source bytes:
            byte[] sourceBytes = await IOUtils.TryReadAllBytesAsync(sourcePath, logger, parallelOptions?.CancellationToken ?? default);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sourceBytes.Length, int.MaxValue, nameof(sourceFileSize));

            // Find EOCD:
            const uint signature = 0x06054B50;
            long sourceEOCDOffset = -1;
            using (MemoryStream sourceMS = new(sourceBytes))
            using (BinaryReader sourceBR = new(sourceMS, Encoding.UTF8, leaveOpen: true))
            {
                sourceMS.Position = Math.Max(0, sourceMS.Length - (22 + 65535));

                while (sourceMS.Position <= sourceMS.Length - 4)
                {
                    uint sig = sourceBR.ReadUInt32LE();
                    if (sig == signature)
                    {
                        sourceEOCDOffset = sourceMS.Position - 4;
                        break;
                    }
                    sourceMS.Position -= 3;
                }
            }
            ArgumentOutOfRangeException.ThrowIfLessThan(sourceEOCDOffset, 0, nameof(sourceEOCDOffset));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceEOCDOffset, sourceBytes.Length - 22, nameof(sourceEOCDOffset));

            //////////////////////////////////////////////
            // Offset   Size   Field
            //   0       4     Signature (0x06054B50)
            //   4       2     Disk number
            //   6       2     Disk with central directory
            //   8       2     Entries on this disk
            //   10      2     Total entries
            //   12      4     Central directory size
            //   16      4     Central directory offset
            //   20      2     Comment length (n)
            //   22      n     Comment
            //////////////////////////////////////////////

            ReadOnlySpan<byte> fields = sourceBytes.AsSpan((int)sourceEOCDOffset);

            ushort sourceDiskNumber = BinaryPrimitives.ReadUInt16LittleEndian(fields.Slice(4, 2));
            ArgumentOutOfRangeException.ThrowIfNotEqual(sourceDiskNumber, 0, nameof(sourceDiskNumber));

            ushort sourceDiskWithCD = BinaryPrimitives.ReadUInt16LittleEndian(fields.Slice(6, 2));
            ArgumentOutOfRangeException.ThrowIfNotEqual(sourceDiskWithCD, 0, nameof(sourceDiskWithCD));

            ushort sourceEntriesOnDisk = BinaryPrimitives.ReadUInt16LittleEndian(fields.Slice(8, 2));
            ushort sourceTotalEntries = BinaryPrimitives.ReadUInt16LittleEndian(fields.Slice(10, 2));
            ArgumentOutOfRangeException.ThrowIfNotEqual(sourceEntriesOnDisk, sourceTotalEntries, nameof(sourceEntriesOnDisk));

            uint sourceCDSize = BinaryPrimitives.ReadUInt32LittleEndian(fields.Slice(12, 4));
            uint sourceCDOffset = BinaryPrimitives.ReadUInt32LittleEndian(fields.Slice(16, 4));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sourceCDOffset, sourceFileSize, nameof(sourceCDOffset));

            // Create target in-memory:
            const int maxFilenameLength = 4096;
            long estimatedTargetSize = sourceBytes.Length + newFiles.Sum(f => f.Length + 2 * maxFilenameLength + 30 + 46); // each additional file: data + 2 * maxFilenameLength + LFE header + CDE header
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(estimatedTargetSize, int.MaxValue, nameof(estimatedTargetSize));
            using MemoryStream targetMS = new((int)estimatedTargetSize);
            using BinaryWriter targetBW = new(targetMS, Encoding.UTF8, leaveOpen: true);

            // Copy the whole exiting LFE section:
            targetMS.Position = 0;
            targetMS.Write(sourceBytes, 0, (int)sourceCDOffset);

            // Add new "Local File Entries":
            List<JarLFE> lfes = [];
            SemaphoreSlim semaphore = new(1, 1);

            // Read files, calculate CRC32, write data:
            baseDir = $"{baseDir.AsStandardPath()}/";

            // Add own cancellation token source to parallel options:
            using CancellationTokenSource ownCTS = new();
            using CancellationTokenSource linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(ownCTS.Token, parallelOptions?.CancellationToken ?? default);
            parallelOptions = parallelOptions.Clone();
            parallelOptions.CancellationToken = linkedCTS.Token;
            CancellationToken ct = linkedCTS.Token;
            try
            {
                await Parallel.ForEachAsync(newFiles, parallelOptions, async (fi, ct) =>
                {
                    if (fi.Length >= int.MaxValue)
                    {
                        logger?.Error($@"File is too big: ""{fi.FullName}""");
                        ownCTS.Cancel();
                        return;
                    }

                    byte[] data = await IOUtils.TryReadAllBytesAsync(fi.FullName, logger, ct);
                    if (data == null)
                    {
                        ownCTS.Cancel();
                        return;
                    }

                    uint crc32 = Crc32.Compute(data);

                    string path = fi.FullName.AsStandardPath();
                    if (!path.StartsWith(baseDir))
                    {
                        logger?.Error($@"File is not within base directory: ""{fi.FullName}""");
                        ownCTS.Cancel();
                        return;
                    }

                    string relativePath = path.Substring(baseDir.Length);
                    if (relativePath.IsWhiteSpace())
                    {
                        logger?.Error($@"Invalid file path: ""{fi.FullName}""");
                        ownCTS.Cancel();
                        return;
                    }

                    byte[] filenameBytes = Encoding.UTF8.GetBytes(relativePath);
                    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(filenameBytes.Length, maxFilenameLength, nameof(relativePath));

                    JarLFE lfe = new()
                    {
                        Filename = relativePath,
                        FilenameBytes = filenameBytes,
                        Data = data,
                        Crc32 = crc32,
                        CompressedSize = (uint)data.Length,
                        UncompressedSize = (uint)data.Length,
                        LastModified = fi.LastWriteTime, // use local time here
                    };

                    // Write LFE, create CDE for later:
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        if (targetMS.Position >= int.MaxValue)
                        {
                            logger?.Error(tooBig);
                            ownCTS.Cancel();
                            return;
                        }
                        lfe.Offset = (uint)targetMS.Position;
                        lfe.Write(targetBW);
                        lfes.Add(lfe);
                    }
                    catch (Exception ex)
                    {
                        logger?.Error(ex);
                        ownCTS.Cancel();
                        return;
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                }); // Parallel.ForEachAsync()
            }
            catch (OperationCanceledException)
            {
                if (ownCTS.IsCancellationRequested)
                    return false;
                throw;
            }

            // Copy existing CDEs:
            ArgumentOutOfRangeException.ThrowIfNotEqual(sourceCDSize, sourceEOCDOffset - sourceCDOffset, nameof(sourceCDSize));
            long targetCDSize = sourceCDSize;

            long targetCDOffset = targetMS.Position;
            if (targetCDOffset + sourceCDSize >= int.MaxValue)
            {
                logger?.Error(tooBig);
                return false;
            }
            targetMS.Write(sourceBytes, (int)sourceCDOffset, (int)sourceCDSize);

            // Write new CDEs:
            foreach (JarLFE lfe in lfes)
            {
                ct.ThrowIfCancellationRequested();

                JarCDE cde = new(lfe);
                targetCDSize += cde.TotalLength;
                if (targetCDSize >= int.MaxValue)
                {
                    logger?.Error(tooBig);
                    return false;
                }
                cde.Write(targetBW);
            }

            ct.ThrowIfCancellationRequested();

            // Copy EOCD:
            long targetEocdOffset = targetMS.Position;
            long sourceEocdSize = sourceBytes.Length - (int)sourceEOCDOffset;
            if (targetEocdOffset + sourceEocdSize >= int.MaxValue)
            {
                logger?.Error(tooBig);
                return false;
            }
            targetMS.Write(sourceBytes, (int)sourceEOCDOffset, sourceBytes.Length - (int)sourceEOCDOffset);

            // Update target OECD fields:
            long targetTotalEntries = lfes.Count + (long)sourceTotalEntries;
            if (targetTotalEntries > ushort.MaxValue)
            {
                logger?.Error($@"Total number of entries ({targetTotalEntries}) exceeds maximum capacity");
                return false;
            }

            ct.ThrowIfCancellationRequested();

            ///////////////////////////////////////////
            // Offset   Size   Field
            //   8       2     Entries on this disk
            //   10      2     Total entries
            //   12      4     Central directory size
            //   16      4     Central directory offset
            ///////////////////////////////////////////

            long targetEndOfStream = targetMS.Position;
            targetMS.Position = targetEocdOffset + 8;
            BinaryX.WriteUInt16LE(targetBW, (ushort)targetTotalEntries);
            BinaryX.WriteUInt16LE(targetBW, (ushort)targetTotalEntries);
            BinaryX.WriteUInt32LE(targetBW, (uint)targetCDSize);
            BinaryX.WriteUInt32LE(targetBW, (uint)targetCDOffset);
            targetMS.Position = targetEndOfStream;

            // Write file:
            targetMS.Position = 0;
            if (!await IOUtils.TryWriteMemoryStreamToFileAsync(targetPath, targetMS, 0, targetEndOfStream, logger, ct))
                return false;

            // Done.
            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }





}
