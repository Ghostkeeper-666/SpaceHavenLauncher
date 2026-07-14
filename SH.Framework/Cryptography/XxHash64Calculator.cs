using SH.Framework.IO;
using SH.Framework.Logging;
using System;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Framework.Cryptography;

public static class XxHash64Calculator
{
    public static string ComputeFromBytes(byte[] bytes, ILogger log)
    {
        try
        {
            if (bytes == null)
                return null;
            using MemoryStream ms = new(bytes, writable: false);
            return ComputeFromStream(ms, log);
        }
        catch (Exception ex)
        {
            log?.Error($@"Unable to compute hash of text: {ex}");
            return null;
        }
    }

    public static string ComputeFromString(string input, ILogger log)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using MemoryStream ms = new(bytes, writable: false);
            return ComputeFromStream(ms, log);
        }
        catch (Exception ex)
        {
            log?.Error($@"Unable to compute hash of text: {ex}");
            return null;
        }
    }

    public static string ComputeFromStream(Stream stream, ILogger log)
    {
        try
        {
            stream.Position = 0;
            XxHash64 hasher = new();
            byte[] buffer = new byte[1024 * 1024];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                hasher.Append(buffer.AsSpan(0, read));
            byte[] hash = hasher.GetHashAndReset();
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            log?.Error($@"Unable to compute hash of stream: {ex}");
            return null;
        }
    }


    public static async Task<string> ComputeFromFileAsync(string path, ILogger log, CancellationToken ct)
    {
        try
        {
            if(!path.FileExists())
                return null;
            await using FileStream sourceStream = File.OpenRead(path);
            return await ComputeFromStreamAsync(sourceStream, log, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error($@"Unable to compute hash of file ""{path}"": {ex}");
            return null;
        }
    }

    public static async Task<string> ComputeFromStreamAsync(Stream stream, ILogger log, CancellationToken ct)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanRead)
                return null;

            if (stream.CanSeek)
                stream.Position = 0;

            XxHash64 hasher = new();
            byte[] buffer = new byte[1024 * 1024];
            int read;
            while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                hasher.Append(buffer.AsSpan(0, read));

            byte[] hash = hasher.GetHashAndReset();
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log?.Error($@"Unable to compute hash of stream: {ex}");
            return null;
        }
    }

}
