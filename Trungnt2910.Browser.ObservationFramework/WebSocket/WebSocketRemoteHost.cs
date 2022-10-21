using System.Reflection;
using System.Runtime.Loader;
using Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

internal class WebSocketRemoteHost : IRemoteHost
{
    private static readonly TimeSpan _maxWaitTime = TimeSpan.FromSeconds(10);

    private IHostProcess? _process;
    private WebSocketProcessServer? _server;
    private WebSocketProcessConnection? _connection;
    private bool _disposedValue;

    public string? FrameworkEnvironment => _connection?.ClientFrameworkEnvironment;

    private WebSocketRemoteHost()
    {
        Console.CancelKeyPress += (sender, args) => Dispose();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => Dispose();
    }

    public static async Task<WebSocketRemoteHost> CreateAsync(IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink)
    {
        while (true)
        {
            var currentHost = new WebSocketRemoteHost();
            currentHost._server = new WebSocketProcessServer()
            {
                DiagnosticMessageSink = diagnosticMessageSink,
                ExecutionMessageSink = executionMessageSink
            };
            currentHost._process = new MicrosoftEdgeWasmHostProcess(currentHost._server.Port);

            diagnosticMessageSink.OnMessage(new DiagnosticMessage()
            {
                Message = $"[{nameof(WebSocketRemoteHost)}]: Listening for remote processes on WebSocket port {currentHost._server.Port}..."
            });

            var connectionTask = currentHost._server.WaitForNextProcessConnection();

            await Task.WhenAny(connectionTask, Task.Delay(_maxWaitTime));

            if (!connectionTask.IsCompleted)
            {
                diagnosticMessageSink.OnMessage(new DiagnosticMessage()
                {
                    Message = $"[{nameof(WebSocketRemoteHost)}]: The remote process did not respond after {_maxWaitTime.TotalMilliseconds}ms. Retrying..."
                });
                currentHost.Dispose();
                continue;
            }

            currentHost._connection = connectionTask.Result;
            currentHost._process.Exited += (sender, args) =>
                currentHost.Dispose();

            diagnosticMessageSink.OnMessage(new DiagnosticMessage()
            {
                Message = $"[{nameof(WebSocketRemoteHost)}]: Successfully connected to remote process ({currentHost.FrameworkEnvironment ?? "Unknown framework"})"
            });

            return currentHost;
        }
    }

    public async Task<bool> AssemblyLoadedOnHost(string assemblyName)
    {
        var message = new QueryAssemblyNameData()
        {
            Op = MessageOperation.QueryAssemblyName,
            AssemblyName = assemblyName
        };

        var result = await _connection!.Send<QueryAssemblyNameResult, QueryAssemblyNameData>(message);

        return result.Exists;
    }

    public async Task LoadAssembly(string assemblyPath)
    {
        var asl = new AssemblyLoadContext("TestLoadContext", isCollectible: true);

        var asm = asl.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));

        await LoadAssembly(asm, asl);

        asl.Unload();
    }

    public async Task LoadAssembly(Assembly assembly, AssemblyLoadContext context)
    {
        if (await AssemblyLoadedOnHost(assembly.FullName!))
        {
            return;
        }

        foreach (var refAssemblyName in assembly.GetReferencedAssemblies())
        {
            var refAssembly = context.LoadFromAssemblyName(refAssemblyName);
            await LoadAssembly(refAssembly, context);
        }

        var message = new LoadAssemblyData()
        {
            Op = MessageOperation.LoadAssembly,
            Assembly = File.ReadAllBytes(assembly.Location),
        };

        var result = await _connection!.Send<LoadAssemblyResult, LoadAssemblyData>(message);

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }
    }

    public async Task InvokeMethod(int testObjectHandle, MethodInfo info)
    {
        var message = new InvokeMethodData()
        {
            Op = MessageOperation.InvokeMethod,
            MethodAssemblyName = info.DeclaringType!.Assembly.FullName!,
            MethodTypeName = info.DeclaringType!.FullName!,
            MethodName = info.Name,
            ObjectHandle = testObjectHandle
        };

        var result = await _connection!.Send<InvokeMethodResult, InvokeMethodData>(message);

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }
    }

    public async Task<int> ConstructTestObject(Type objectType)
    {
        var message = new ConstructObjectData()
        {
            Op = MessageOperation.ConstructObject,
            TypeAssemblyName = objectType.Assembly.FullName!,
            TypeName = objectType.FullName!
        };

        var result = await _connection!.Send<ConstructObjectResult, ConstructObjectData>(message);

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }

        return result.ObjectHandle;
    }

    public async Task DisposeTestObject(int handle)
    {
        var message = new DisposeObjectData()
        {
            Op = MessageOperation.DisposeObject,
            ObjectHandle = handle
        };

        var result = await _connection!.Send<DisposeObjectResult, DisposeObjectData>(message);

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _process?.Dispose();
                _server?.Dispose();
                _connection?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~RemoteHost()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
