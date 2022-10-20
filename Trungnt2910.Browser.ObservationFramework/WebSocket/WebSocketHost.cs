using ObservationFramework.WebSocket.Messages;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Trungnt2910.Browser.ObservationFramework.WebSocket;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ObservationFramework.WebSocket;

internal class WebSocketHost : WebSocketBehavior
{
    private static WebSocketServer _server;
    private static int _serverPort = WebSocketSettings.Port;
    private static TaskCompletionSource _waitForTestHost = new();
    private static Dictionary<Guid, TaskCompletionSource<MessageResult>> _pendingResults = new();
    private static WebSocketHost _client = new();
    private static HttpFileServer _wasmPageServer;
    private static string _runtimeFolder;
    private static Process _runtimeProcess;

    static WebSocketHost()
    {
        _serverPort = TcpHelpers.FindUnusedPort();
        _server = new($"{WebSocketSettings.Scheme}{WebSocketSettings.Host}:{_serverPort}");
        _server.AddWebSocketService<WebSocketHost>($"/{WebSocketSettings.Path}");
        _server.Start();
        _server.WaitTime = WebSocketSettings.WaitTime;
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

        _runtimeProcess = Process.Start(new ProcessStartInfo()
        {
            FileName = "msedge",
            Arguments = $"--user-data-dir={edgeDataFolder} --auto-open-devtools-for-tabs --disable-extensions \"http://localhost:{_wasmPageServer.Port}/?{WebSocketSettings.WebSocketPortQueryString}={_serverPort}\"",
            UseShellExecute = true
        })!;
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        var data = WebSocketMessageSerializer.Deserialize<MessageData>(e.RawData)!;

        switch (data.Op)
        {
            case MessageOperation.Connect:
                _client = this;
                _waitForTestHost.SetResult();
                break;
            default:
                _pendingResults.GetValueOrDefault(data.Id)?.SetResult((MessageResult)data);
                break;
        }
    }

    public static Task WaitForTestHost()
    {
        return _waitForTestHost.Task;
    }

    public static async Task<bool> AssemblyLoadedOnHost(string assemblyName)
    {
        var query = new QueryAssemblyNameData()
        {
            Op = MessageOperation.QueryAssemblyName,
            AssemblyName = assemblyName
        };

        var tcs = new TaskCompletionSource<MessageResult>();
        _pendingResults.Add(query.Id, tcs);

        Send(query);

        var result = (QueryAssemblyNameResult)await tcs.Task;

        return result.Exists;
    }

    public static async Task LoadAssemblyOnHost(string assemblyPath)
    {
        var asl = new AssemblyLoadContext("TestLoadContext", isCollectible: true);

        var asm = asl.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));

        await LoadAssemblyOnHost(asm, asl);

        asl.Unload();
    }

    public static async Task LoadAssemblyOnHost(Assembly assembly, AssemblyLoadContext context)
    {
        if (await AssemblyLoadedOnHost(assembly.FullName!))
        {
            return;
        }

        foreach (var refAssemblyName in assembly.GetReferencedAssemblies())
        {
            var refAssembly = context.LoadFromAssemblyName(refAssemblyName);
            await LoadAssemblyOnHost(refAssembly, context);
        }

        var message = new LoadAssemblyData()
        {
            Op = MessageOperation.LoadAssembly,
            Assembly = File.ReadAllBytes(assembly.Location),
        };

        var tcs = new TaskCompletionSource<MessageResult>();
        _pendingResults.Add(message.Id, tcs);

        Send(message);

        var result = (LoadAssemblyResult)await tcs.Task;

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }
    }

    public static async Task InvokeMethodOnHost(int testObjectHandle, MethodInfo info)
    {
        var message = new InvokeMethodData()
        {
            Op = MessageOperation.InvokeMethod,
            MethodAssemblyName = info.DeclaringType!.Assembly.FullName!,
            MethodTypeName = info.DeclaringType!.FullName!,
            MethodName = info.Name,
            ObjectHandle = testObjectHandle
        };

        var tcs = new TaskCompletionSource<MessageResult>();
        _pendingResults.Add(message.Id, tcs);

        Send(message);

        var result = (InvokeMethodResult)await tcs.Task;

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }
    }

    public static async Task<int> ConstructTestObjectOnHost(Type objectType)
    {
        var message = new ConstructObjectData()
        {
            Op = MessageOperation.ConstructObject,
            TypeAssemblyName = objectType.Assembly.FullName!,
            TypeName = objectType.FullName!
        };

        var tcs = new TaskCompletionSource<MessageResult>();
        _pendingResults.Add(message.Id, tcs);

        Send(message);

        var result = (ConstructObjectResult)await tcs.Task;

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }

        return result.ObjectHandle;
    }

    public static async Task DisposeTestObjectOnHost(int handle)
    {
        var message = new DisposeObjectData()
        {
            Op = MessageOperation.DisposeObject,
            ObjectHandle = handle
        };

        var tcs = new TaskCompletionSource<MessageResult>();
        _pendingResults.Add(message.Id, tcs);

        Send(message);

        var result = (DisposeObjectResult)await tcs.Task;

        if (result.Exception != null)
        {
            throw new HostExecutionException(result.Exception);
        }
    }

    public static void Send(MessageData data)
    {
        _client.Send(WebSocketMessageSerializer.Serialize(data));
    }

    public static void CleanTestHost()
    {
        _client = new();
        _waitForTestHost = new();
        _pendingResults.Clear();
        _runtimeProcess.Kill();
        Directory.Delete(_runtimeFolder, true);
    }
}
