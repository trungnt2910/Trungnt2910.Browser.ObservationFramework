using Gapotchenko.FX.Diagnostics;
using ObservationFramework.WebSocket;
using System.Diagnostics;
using System.Reflection;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

class MicrosoftEdgeWasmHostProcess : IHostProcess
{
    const int _maxEdgeSearchAttempts = 100;

    private Process? _runtimeProcess;
    private string _runtimeFolder;
    private HttpFileServer _wasmPageServer;

    public MicrosoftEdgeWasmHostProcess(int webSocketPort)
    {
        _runtimeFolder = Path.Combine(Path.GetTempPath(), $"observationframework-{Guid.NewGuid()}");
        var hostFolder = Path.Combine(_runtimeFolder, "host");
        var edgeDataFolder = Path.Combine(_runtimeFolder, "edgedata");

        var asm = Assembly.GetExecutingAssembly();
        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            using var resourceStream = asm.GetManifestResourceStream(resourceName)!;
            var fileName = Path.Combine(hostFolder, resourceName);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
            using var fileStream = File.Create(fileName);
            resourceStream.CopyTo(fileStream);
        }

        _wasmPageServer = new(hostFolder);

        Directory.CreateDirectory(edgeDataFolder);

        File.WriteAllText(Path.Combine(edgeDataFolder, "FirstLaunchAfterInstallation"), "");
        File.WriteAllText(Path.Combine(edgeDataFolder, "Local State"), @"
        {
            ""fre"":{
                ""has_first_visible_browser_session_completed"":true,
                ""has_user_committed_selection_to_import_during_fre"":false,
                ""has_user_completed_fre"":false,
                ""has_user_seen_fre"":true,
                ""last_seen_fre"":""106.0.1370.47"",
                ""oem_bookmarks_set"":true
            }
        }
        ");

        var edgeParams = $"--user-data-dir={edgeDataFolder} --auto-open-devtools-for-tabs --disable-extensions \"http://localhost:{_wasmPageServer.Port}/?{WebSocketSettings.WebSocketPortQueryString}={webSocketPort}\"";

        if (OperatingSystem.IsWindows())
        {
            // Workaround for Windows: Sometimes (especially during GitHub Actions runs) the Edge process
            // just hangs there when started using `Process.Start`.
            var cmdProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/K \"\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\" {edgeParams}",
                UseShellExecute = false,
            });

            if (cmdProcess != null)
            {
                for (int i = 0; i < _maxEdgeSearchAttempts; ++i)
                {
                    foreach (var proc in Process.GetProcessesByName("msedge"))
                    {
                        try
                        {
                            if (proc.GetParent()?.Id == cmdProcess.Id)
                            {
                                _runtimeProcess = proc;
                                goto foundEdge;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                foundEdge:
                    cmdProcess.Kill();
            }
        }

        // The workaround above may or may not work somehow, proceed to the second method.
        if (_runtimeProcess == null)
        {
            _runtimeProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = "msedge",
                Arguments = $"{edgeParams}",
                UseShellExecute = true
            });
        }

        if (_runtimeProcess == null)
        {
            throw new HostExecutionException("Failed to launch Microsoft Edge.");
        }

        _runtimeProcess.EnableRaisingEvents = true;
    }

    public event EventHandler Exited 
    {
        add
        {
            if (_runtimeProcess != null)
            {
                _runtimeProcess.Exited += value;
            }
        }
        remove
        {
            if (_runtimeProcess != null)
            {
                _runtimeProcess.Exited -= value;
            }
        }
    }

    public void Stop()
    {
        try
        {
            _runtimeProcess?.Kill(true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to kill runtime process: {ex}");
        }

        _wasmPageServer.Stop();

        try
        {
            Directory.Delete(_runtimeFolder, true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to delete runtime folder: {ex}");
        }
    }

    #region IDisposable
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            Stop();
            disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~MicrosoftEdgeWasmHostProcess()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
