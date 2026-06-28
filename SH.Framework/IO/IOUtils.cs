using SH.Framework.Extensions;
using SH.Framework.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SH.Framework.IO;

public static class IOUtils
{
    public static string CombinePath(string basePath, string relativePath)
    {
        try { return Path.Combine(basePath ?? string.Empty, relativePath ?? string.Empty); }
        catch { return relativePath ?? basePath; }
    }

    public static string CombineAsOSPath(string basePath, string relativePath)
    {
        try { return Path.Combine(basePath.AsOSPath(), relativePath.AsOSPath()).AsOSPath(); }
        catch { return relativePath ?? basePath; }
    }

    public static string CombineAsStdPath(string basePath, string relativePath)
    {
        try { return Path.Combine(basePath.AsStdPath(), relativePath.AsStdPath()).AsStdPath(); }
        catch { return relativePath ?? basePath; }
    }

    public static bool FileExists(string path)
    {
        try { return File.Exists(path ?? string.Empty); }
        catch { return false; }
    }

    public static bool DirectoryExists(string path)
    {
        try { return Directory.Exists(path ?? string.Empty); }
        catch { return false; }
    }

    public static void ThrowIfFileNotExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path.Trim()))
            throw new FileNotFoundException($@"File does not exist: ""{path}""", path);
    }

    public static void ThrowIfDirectoryNotExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Directory.Exists(path.Trim()))
            throw new FileNotFoundException($@"Directory does not exist: ""{path}""", path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsOSPath(this string path) =>
        string.IsNullOrWhiteSpace(path) ? string.Empty :
        OS.IsWin ? path.Replace('/', '\\').Trim().TrimEnd('\\') :
        path.Replace('\\', '/').Trim().TrimEnd('/');


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsStdPath(this string path) =>
        string.IsNullOrWhiteSpace(path) ? string.Empty :
        path.Replace('\\', '/').Trim().TrimEnd('/');


    public static async Task<bool> TryReadFirstBytesAsync(string path, int startPos, byte[] bytes, ILogger logger)
    {
        try
        {
            path = path.AsOSPath();
            ThrowIfFileNotExists(path);
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentOutOfRangeException.ThrowIfLessThan(startPos, 0, nameof(startPos));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startPos, bytes.Length, nameof(startPos));

            int bufferLength = bytes.Length - startPos;
            await using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: bufferLength, useAsync: true);
            long fileLength = fs.Length;

            int size = (int)Math.Min(fileLength, bufferLength);
            if (size == 0)
                return true;

            int read = await fs.ReadAtLeastAsync(bytes.AsMemory(startPos, size), size, throwOnEndOfStream: false).ConfigureAwait(false);
            return read == size;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error($@"Unable to read first bytes of file ""{path}"": {ex}");
            return false;
        }
    }


    public static async Task<XDocument> TryLoadXDocumentAsync(string path, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();
            if (path.IsNullOrWhiteSpace() || !File.Exists(path))
            {
                logger?.Error($@"XML document could not be found: ""{path}""", Path.GetDirectoryName(path));
                return null;
            }
            ct.ThrowIfCancellationRequested();
            string xml = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return XDocument.Parse(xml, LoadOptions.SetLineInfo | LoadOptions.SetLineInfo);
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return null;
        }
    }
    public static async Task<bool> TrySaveXDocumentAsync(string path, XDocument doc, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();
            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !await TryCreateDirectoryAsync(dir, logger, ct))
                return false;

            XmlWriterSettings settings = new()
            {
                Async = true,

                OmitXmlDeclaration = true,
                Encoding = new UTF8Encoding(false),
                ConformanceLevel = ConformanceLevel.Document,

                Indent = true,
                IndentChars = "  ",
                CheckCharacters = true,

                NewLineChars = "\n",
                NewLineOnAttributes = false,
                NewLineHandling = NewLineHandling.Replace,
            };

            await using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await using XmlWriter writer = XmlWriter.Create(stream, settings);
            ct.ThrowIfCancellationRequested();
            await doc.SaveAsync(writer, ct);

            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return false;
        }
    }



    public static bool TryCreateDirectory(string directory, out string error)
    {
        try
        {
            directory = directory.AsOSPath();
            DirectoryInfo dir = Directory.CreateDirectory(directory);
            error = null;
            return dir.Exists;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static bool TryCreateDirectory(string directory, ILogger logger)
    {
        directory = directory.AsOSPath();
        if (TryCreateDirectory(directory, out string error))
            return true;
        string parent = null;
        try { parent = Path.GetDirectoryName(directory).AsOSPath(); } catch { }
        logger?.Error(error, parent);
        return false;
    }
    public static Task<bool> TryCreateDirectoryAsync(string directory, ILogger logger, CancellationToken ct) =>
        Task.Run(() => TryCreateDirectory(directory, logger), ct);



    public static bool TryDeleteDirectory(string directory, out string error)
    {
        try
        {
            directory = directory.AsOSPath();
            if (!Directory.Exists(directory))
            {
                error = null;
                return true;
            }
            Directory.Delete(directory, true);
            error = null;
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static bool TryDeleteDirectory(string directory, ILogger logger)
    {
        directory = directory.AsOSPath();
        if (TryDeleteDirectory(directory, out string error))
            return true;
        string parent = null;
        try { parent = Path.GetDirectoryName(directory); } catch { }
        logger?.Error(error, parent);
        return false;
    }
    public static Task<bool> TryDeleteDirectoryAsync(string directory, ILogger logger, CancellationToken ct) =>
        Task.Run(() => TryDeleteDirectory(directory, logger), ct);



    public static bool TryDeleteFile(string path, out string error)
    {
        try
        {
            path = path.AsOSPath();
            error = null;
            if (!File.Exists(path))
                return true;
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static bool TryDeleteFile(string path, ILogger logger)
    {
        path = path.AsOSPath();
        if (TryDeleteFile(path, out string error))
            return true;
        string parent = null;
        try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
        logger?.Error(error, parent);
        return false;
    }
    public static Task<bool> TryDeleteFileAsync(string path, ILogger logger, CancellationToken ct) =>
        Task.Run(() => TryDeleteFile(path, logger), ct);



    public static bool TryWriteAllText(string path, string text, out string error)
    {
        try
        {
            path = path.AsOSPath();
            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !TryCreateDirectory(dir, out error))
                return false;
            File.WriteAllText(path, text ?? string.Empty);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static async Task<bool> TryWriteAllTextAsync(string path, string text, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();
            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !await TryCreateDirectoryAsync(Path.GetDirectoryName(path), logger, ct))
                return false;
            await File.WriteAllTextAsync(path, text ?? string.Empty, ct);
            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return false;
        }
    }

    public static bool TryReadAllText(string path, out string text, out string error)
    {
        try
        {
            path = path.AsOSPath();
            if (path.IsNullOrWhiteSpace() || !File.Exists(path))
            {
                error = $@"Unable to read text file, invalid path: ""{path}""";
                text = null;
                return false;
            }
            text = File.ReadAllText(path);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            text = null;
            return false;
        }
    }
    public static bool TryReadAllText(string path, out string text, ILogger logger = null)
    {
        path = path.AsOSPath();
        if (TryReadAllText(path, out text, out string error))
            return true;
        string parent = null;
        try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
        logger?.Error(error, parent);
        return false;
    }
    public static async Task<string> TryReadAllTextAsync(string path, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();
            if (path.IsNullOrWhiteSpace() || !File.Exists(path))
                return null;
            return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return null;
        }
    }

    public static bool TryAppendAllText(string absolutePath, string text, out string error)
    {
        try
        {
            absolutePath = absolutePath.AsOSPath();
            string dir = Path.GetDirectoryName(absolutePath);
            if (!dir.IsNullOrWhiteSpace() && !TryCreateDirectory(Path.GetDirectoryName(absolutePath), out error))
                return false;
            File.AppendAllText(absolutePath, text ?? string.Empty);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static async Task<bool> TryAppendAllTextAsync(string absolutePath, string text, ILogger logger, CancellationToken ct)
    {
        try
        {
            absolutePath = absolutePath.AsOSPath();
            string dir = Path.GetDirectoryName(absolutePath);
            if (!dir.IsNullOrWhiteSpace() && !await TryCreateDirectoryAsync(dir, logger, ct))
                return false;
            await File.AppendAllTextAsync(absolutePath, text ?? string.Empty, ct);
            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }




    public static bool TryWriteAllBytes(string path, byte[] bytes, out string error)
    {
        try
        {
            path = path.AsOSPath();
            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !TryCreateDirectory(dir, out error))
                return false;
            File.WriteAllBytes(path, bytes ?? Array.Empty<byte>());
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static async Task<bool> TryWriteAllBytesAsync(string path, byte[] bytes, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();
            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !await TryCreateDirectoryAsync(Path.GetDirectoryName(path), logger, ct))
                return false;
            await File.WriteAllBytesAsync(path, bytes ?? Array.Empty<byte>(), ct);
            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return false;
        }
    }

    public static bool TryReadAllBytes(string path, out byte[] bytes, out string error)
    {
        try
        {
            path = path.AsOSPath();
            if (path.IsNullOrWhiteSpace() || !File.Exists(path))
            {
                error = $@"Unable to read file, invalid path: ""{path}""";
                bytes = null;
                return false;
            }
            bytes = File.ReadAllBytes(path);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            bytes = null;
            return false;
        }
    }
    public static bool TryReadAllBytes(string path, out byte[] bytes, ILogger logger = null)
    {
        path = path.AsOSPath();
        if (TryReadAllBytes(path, out bytes, out string error))
            return true;
        string parent = null;
        try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
        logger?.Error(error, parent);
        return false;
    }
    public static async Task<byte[]> TryReadAllBytesAsync(string path, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();
            if (path.IsNullOrWhiteSpace() || !File.Exists(path))
                return null;
            return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return null;
        }
    }

    public static bool TryAppendAllBytes(string absolutePath, byte[] bytes, out string error)
    {
        try
        {
            absolutePath = absolutePath.AsOSPath();
            string dir = Path.GetDirectoryName(absolutePath);
            if (!dir.IsNullOrWhiteSpace() && !TryCreateDirectory(Path.GetDirectoryName(absolutePath), out error))
                return false;
            File.AppendAllBytes(absolutePath, bytes ?? Array.Empty<byte>());
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static async Task<bool> TryAppendAllBytesAsync(string absolutePath, byte[] bytes, ILogger logger, CancellationToken ct)
    {
        try
        {
            absolutePath = absolutePath.AsOSPath();
            string dir = Path.GetDirectoryName(absolutePath);
            if (!dir.IsNullOrWhiteSpace() && !await TryCreateDirectoryAsync(dir, logger, ct))
                return false;
            await File.AppendAllBytesAsync(absolutePath, bytes ?? Array.Empty<byte>(), ct);
            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return false;
        }
    }





    public static bool TextFileContains(string path, string searchText, int withinMaxLength, bool ignoreWhitespaces, StringComparison stringComparison)
    {
        try
        {
            path = path.AsOSPath();
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentException.ThrowIfNullOrEmpty(searchText);
            using StreamReader reader = new(path);
            char[] buffer = new char[withinMaxLength]; // reads 4x more than necessary
            int read = reader.Read(buffer, 0, withinMaxLength);
            if (read < searchText.Length)
                return false;
            string text = new(buffer, 0, read);
            if (ignoreWhitespaces)
                text = text.TrimStart();
            return text.Contains(searchText, stringComparison);
        }
        catch { return false; }
    }



    public static bool TryCopyFile(string source, string target, bool overwrite, out string error)
    {
        try
        {
            source = source.AsOSPath();
            target = target.AsOSPath();
            string targetDir = Path.GetDirectoryName(target);
            if (!TryCreateDirectory(targetDir, out error))
                return false;
            File.Copy(source, target, overwrite);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }
    public static bool TryCopyFile(string source, string target, bool overwrite, ILogger logger)
    {
        if (TryCopyFile(source, target, overwrite, out string error))
            return true;
        string parent = null;
        try { parent = Path.GetDirectoryName(source).AsOSPath(); } catch { }
        logger?.Error(error, parent);
        return false;
    }
    public static async Task<bool> TryCopyFileAsync(string sourcePath, string targetPath, bool overwrite, ILogger logger, CancellationToken ct)
    {
        try
        {
            sourcePath = sourcePath.AsOSPath();
            targetPath = targetPath.AsOSPath();
            if (ct.IsCancellationRequested)
                return false;
            string targetDir = Path.GetDirectoryName(targetPath);
            if (!await TryCreateDirectoryAsync(targetDir, logger, ct))
                return false;
            const int bufferSize = 64 * 1024;
            FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
            await using FileStream source = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
            await using FileStream target = new(targetPath, fileMode, FileAccess.Write, FileShare.None, bufferSize, true);
            await source.CopyToAsync(target, bufferSize, ct);
            return true;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(sourcePath).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return false;
        }
    }



    public static async Task<bool> TryCopyDirectoryAsync(string source, string target, bool overwrite, ILogger logger, ParallelOptions parallelOptions)
    {
        try
        {
            // Source Dir:
            source = source.AsOSPath();
            if (source.IsNullOrWhiteSpace() || !Directory.Exists(source))
            {
                logger?.Error($@"Source directory not found: ""{source}""");
                return false;
            }
            DirectoryInfo sourceDirInfo = new(source);

            // Target Dir:
            target = target.AsOSPath();
            if (target.IsNullOrWhiteSpace())
            {
                logger?.Error($@"Target directory is not defined");
                return false;
            }
            if (!await TryCreateDirectoryAsync(target, logger, parallelOptions?.CancellationToken ?? default))
                return false;

            // Collect copy information - we intentionally ignore if something changes later on:
            DirectoryInfo[] subDirs = sourceDirInfo.GetDirectories("*", SearchOption.AllDirectories).ToArray();
            FileInfo[] files = sourceDirInfo.GetFiles("*", SearchOption.AllDirectories).ToArray();

            // Create subdirectories:
            foreach (DirectoryInfo sourceSubDirInfo in subDirs)
                if (!await TryCreateDirectoryAsync(Path.Combine(target, Path.GetRelativePath(source, sourceSubDirInfo.FullName)), logger, parallelOptions?.CancellationToken ?? default))
                    return false;

            // Add own cancellation token source to parallel options:
            using CancellationTokenSource ownCTS = new();
            using CancellationTokenSource linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(ownCTS.Token, parallelOptions?.CancellationToken ?? default);
            parallelOptions = parallelOptions.Clone();
            parallelOptions.CancellationToken = linkedCTS.Token;
            CancellationToken ct = linkedCTS.Token;

            // Copy files:
            await Parallel.ForEachAsync(files, parallelOptions, async (sourceFileInfo, ct) =>
            {
                if (!await TryCopyFileAsync(sourceFileInfo.FullName, Path.Combine(target, Path.GetRelativePath(source, sourceFileInfo.FullName)), true, logger, ct))
                    ownCTS.Cancel(); // stops immediately when any fails
            });

            // Done.
            return !ct.IsCancellationRequested;
        }
        catch(OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(source).AsOSPath(); } catch { }
            logger?.Error(ex, parent);
            return false;
        }
    }

    public static async Task<bool> TryWriteMemoryStreamToFileAsync(string path, MemoryStream ms, long start, long count, ILogger logger, CancellationToken ct)
    {
        try
        {
            path = path.AsOSPath();

            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(ms);

            string dir = Path.GetDirectoryName(path);
            if (!dir.IsNullOrWhiteSpace() && !await TryCreateDirectoryAsync(Path.GetDirectoryName(path), logger, ct))
                return false;

            if (start < 0 || count < 0 || start >= ms.Length || count > ms.Length || start > ms.Length - count)
            {
                logger?.Error($"Invalid start ({start}) and count ({count}) for the stream length ({ms.Length})");
                return false;
            }

            ms.Position = start;

            await using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

            if (ms.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                await fs.WriteAsync(buffer.AsMemory(0, (int)count), ct);
            }
            else
            {
                byte[] bytes = ms.ToArray();
                await fs.WriteAsync(bytes.AsMemory(0, (int)count), ct);
            }

            await fs.FlushAsync(ct);

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
