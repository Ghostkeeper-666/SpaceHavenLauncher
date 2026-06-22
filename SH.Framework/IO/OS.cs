using SH.Framework.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Framework.IO;

public static class OS
{
    public static readonly EOSType Type;
    public static readonly bool IsWin;
    public static readonly bool IsMac;
    public static readonly bool IsLnx;

    public static bool IsUnsupported => !(IsWin || IsMac || IsLnx);

    static OS()
    {
        IsWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        IsLnx = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        Type =
            IsWin ? EOSType.Windows :
            IsMac ? EOSType.OSX :
            IsLnx ? EOSType.Linux :
            EOSType.Unsupported;
    }

    public static async Task<bool> TryStartApplication(string path, ILogger logger, CancellationToken ct)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = path,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Maximized, // <<< not respected on all platforms!
            };

            using Process process = new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            if (!process.Start())
            {
                logger?.Error($@"Failed to start process: ""{path}""");
                return false;
            }

            TaskCompletionSource<bool> tcs = new();

            process.Exited += (_, __) =>
            {
                try
                {
                    tcs.TrySetResult(process.ExitCode == 0);
                }
                catch
                {
                    tcs.TrySetResult(false);
                }
            };

            using (ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch { }

                tcs.TrySetResult(false);
            }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            logger?.Error($@"Process ""path"" was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            logger?.Error($@"Error executing process ""{path}"": {ex}");
            return false;
        }
    }



    public static Task OpenDirectoryAsync(string directoryPath, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            return Task.CompletedTask;

        // Fire and forget:
        return Task.Run(() =>
        {
            try
            {
                switch (Type)
                {
                    case EOSType.Windows:
                        Process.Start(new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = "explorer",
                            Arguments = $@"""{directoryPath}""",
                        });
                        return;

                    case EOSType.OSX:
                        if (directoryPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                        {
                            // Reveal .app bundle instead of launching
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "open",
                                ArgumentList = { "-R", directoryPath }
                            });
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "open",
                                ArgumentList = { directoryPath }
                            });
                        }
                        return;

                    case EOSType.Linux:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            ArgumentList = { directoryPath },
                        });
                        return;

                    default:
                        throw new NotImplementedException($"{nameof(EOSType)}={Type}");
                }
            }
            catch (Exception ex)
            {
                log?.Debug(ex);
            }
        });
    }

    public static Task OpenFileAsync(string filePath, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.CompletedTask;

        // Fire and forget:
        return Task.Run(() =>
        {
            try
            {
                switch (Type)
                {
                    case EOSType.Windows:
                        Process.Start(new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = filePath,
                        });
                        return;

                    case EOSType.OSX:
                        if (filePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "open",
                                ArgumentList = { filePath },
                            });
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "xdg-open",
                                ArgumentList = { filePath },
                            });
                        }
                        return;

                    case EOSType.Linux:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            ArgumentList = { filePath },
                        });
                        return;

                    default:
                        throw new NotImplementedException($"{nameof(EOSType)}={Type}");
                }
            }
            catch (Exception ex)
            {
                log?.Debug(ex);
            }
        });
    }

    public static Task OpenHttpAsync(string httpUrl, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(httpUrl))
            return Task.CompletedTask;

        if (!httpUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !httpUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        // Fire and forget:
        return Task.Run(() =>
        {
            try
            {
                switch (Type)
                {
                    case EOSType.Windows:
                        Process.Start(new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = httpUrl,
                        });
                        return;

                    case EOSType.OSX:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "open",
                            ArgumentList = { httpUrl },
                        });
                        return;

                    case EOSType.Linux:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            ArgumentList = { httpUrl },
                        });
                        return;

                    default:
                        throw new NotImplementedException($"{nameof(EOSType)}={Type}");
                }
            }
            catch (Exception ex)
            {
                log?.Debug(ex);
            }
        });
    }

    public static Task OpenLinkAsync(string baseDir, string link, ILogger log)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(link))
                return Task.CompletedTask;

            if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return OpenHttpAsync(link, log);

            string absolute = Path.Combine(baseDir, link).AsStandardPath().TrimEnd('/').AsOSPath();

            if (Directory.Exists(absolute))
                return OpenDirectoryAsync(absolute, log);

            if (File.Exists(absolute))
                return OpenFileAsync(absolute, log);
        }
        catch (Exception ex)
        {
            log?.Debug(ex);
        }
        return Task.CompletedTask;
    }

}
