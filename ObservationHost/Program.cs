using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using ObservationFramework.WebSocket;
using ObservationFramework.WebSocket.Messages;
using ObservationHost;
using System.Net.WebSockets;
using System.Reflection;
using Trungnt2910.Browser.Dom;
using Xunit;

const BindingFlags BindingFlagsAllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

var testObjectCache = new Dictionary<int, object>();
var additionalLoadedAssemblies = new List<Assembly>();

AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

using var ws = new ClientWebSocket();

int webSocketPort = WebSocketSettings.Port;

#if BROWSER
var query = QueryHelpers.ParseQuery(Window.Instance?.Location?.Search ?? string.Empty);
if (query.TryGetValue(WebSocketSettings.WebSocketPortQueryString, out var portQueryValue))
{
    if (int.TryParse(portQueryValue.FirstOrDefault(), out var tempPort))
    {
        webSocketPort = tempPort;
    }
}
#endif

string webSocketUrl = $"{WebSocketSettings.Scheme}{WebSocketSettings.Host}:{webSocketPort}/{WebSocketSettings.Path}";

Console.WriteLine($"[ObservationHost] WebSocket connecting to {webSocketUrl}...");

while (true)
{
    try
    {
        await ws.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ObservationHost] Failed to connect to WebSocket: {ex}. Retrying...");
        continue;
    }

    break;
}

Console.WriteLine($"[ObservationHost] WebSocket connected to {webSocketUrl}.");

Ws_Send(new MessageData() { Op = MessageOperation.Connect });

Console.WriteLine($"[ObservationHost] Sent initialization message to {webSocketUrl}.");

// Keep this type available on the host.
Assert.True(true);

byte[] buffer = new byte[1024 * 1024];

while (true)
{
    while (ws.State == WebSocketState.Open)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            Ws_OnClose();
        }
        else if (result.MessageType == WebSocketMessageType.Binary)
        {
            using var ms = new MemoryStream();
            ms.Write(buffer, 0, result.Count);

            while (!result.EndOfMessage)
            {
                result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                ms.Write(buffer, 0, result.Count);
            }

            Ws_OnMessage(ms.ToArray());
        }
    }
}

void Ws_OnClose()
{
    Environment.Exit(0);
}

void Ws_Send(MessageData data)
{
    var bytes = WebSocketMessageSerializer.Serialize(data);
    _ = ws!.SendAsync(bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
}

async void Ws_OnMessage(byte[] byteData)
{
    var data = WebSocketMessageSerializer.Deserialize<MessageData>(byteData)!;

    if (data.Op != MessageOperation.LoadAssembly)
    {
        Console.WriteLine($"[ObservationHost] Received operation: {JsonConvert.SerializeObject(data)}");
    }

    switch (data.Op)
    {
        case MessageOperation.QueryAssemblyName:
            Ws_Send(QueryAssemblyName((QueryAssemblyNameData)data));
        break;
        case MessageOperation.LoadAssembly:
            Ws_Send(LoadAssembly((LoadAssemblyData)data));
        break;
        case MessageOperation.InvokeMethod:
            Ws_Send(await InvokeMethod((InvokeMethodData)data));
        break;
        case MessageOperation.ConstructObject:
            Ws_Send(ConstructObject((ConstructObjectData)data));
        break;
        case MessageOperation.DisposeObject:
            Ws_Send(DisposeObject((DisposeObjectData)data));
        break;
    }
}

QueryAssemblyNameResult QueryAssemblyName(QueryAssemblyNameData data)
{
    var name = new AssemblyName(data.AssemblyName);
    var loaded = AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), name))
        .Any();

    return new QueryAssemblyNameResult()
    {
        Op = data.Op,
        Id = data.Id,
        Exists = loaded
    };
}

LoadAssemblyResult LoadAssembly(LoadAssemblyData data)
{
    var result = new LoadAssemblyResult()
    {
        Op = data.Op,
        Id = data.Id
    };

    try
    {
        var asm = AppDomain.CurrentDomain.Load(data.Assembly);
        additionalLoadedAssemblies.Add(asm);
        Console.WriteLine($"[ObservationHost] Loaded {asm.FullName} from WebSocket connection.");
    }
    catch (Exception ex)
    {
        result.Exception = new ExceptionMessage(ex);
    }

    return result;
}

async Task<InvokeMethodResult> InvokeMethod(InvokeMethodData data)
{
    var result = new InvokeMethodResult()
    {
        Op = data.Op,
        Id = data.Id
    };

    try
    {
        if (!testObjectCache.TryGetValue(data.ObjectHandle, out var testObject))
        {
            throw new InvalidOperationException($"Trying to invoke method [{data.MethodAssemblyName}] {data.MethodTypeName}:{data.MethodName} on an invalid object handle: {data.ObjectHandle}.");
        }

        var methodAsm = Helpers.GetAssemblyByName(data.MethodAssemblyName);
        var methodType = methodAsm.GetType(data.MethodTypeName)!;
        var methodInfo = methodType.GetMethod(data.MethodName, BindingFlagsAllInstance)!;

        await Helpers.InvokeMethod(testObject, methodInfo);
    }
    catch (Exception ex)
    {
        if (ex is TargetInvocationException && ex.InnerException != null)
        {
            result.Exception = new ExceptionMessage(ex.InnerException);
        }
        else
        {
            result.Exception = new ExceptionMessage(ex);
        }
    }

    return result;
}

ConstructObjectResult ConstructObject(ConstructObjectData data)
{
    var result = new ConstructObjectResult()
    {
        Op = data.Op,
        Id = data.Id
    };

    try
    {
        var testObject = Helpers.GetAssemblyByName(data.TypeAssemblyName).CreateInstance(data.TypeName);

        var onStartMethod = testObject?.GetType().GetMethod("OnStart", BindingFlagsAllInstance);

        if (testObject == null || onStartMethod == null)
        {
            throw new InvalidOperationException($"The test class [{data.TypeAssemblyName}] {data.TypeName} is not valid. Make sure your test class is not static and inherits from Specification.");
        }

        onStartMethod.Invoke(testObject, null);

        result.ObjectHandle = testObject.GetHashCode();
        testObjectCache.Add(result.ObjectHandle, testObject);
    }
    catch (Exception ex)
    {
        if (ex is TargetInvocationException && ex.InnerException != null)
        {
            result.Exception = new ExceptionMessage(ex.InnerException);
        }
        else
        {
            result.Exception = new ExceptionMessage(ex);
        }
    }

    return result;
}

DisposeObjectResult DisposeObject(DisposeObjectData data)
{
    var result = new DisposeObjectResult()
    {
        Op = data.Op,
        Id = data.Id
    };

    try
    {
        if (!testObjectCache.TryGetValue(data.ObjectHandle, out var testObject))
        {
            throw new InvalidOperationException($"Trying to destroy an invalid object handle: {data.ObjectHandle}.");
        }

        var onFinishMethod = testObject.GetType().GetMethod("OnFinish", BindingFlagsAllInstance)!;
        onFinishMethod.Invoke(testObject, null);

        if (testObject is IDisposable disposable)
        {
            disposable.Dispose();
        }

        testObjectCache.Remove(data.ObjectHandle);
    }
    catch (Exception ex)
    {
        if (ex is TargetInvocationException && ex.InnerException != null)
        {
            result.Exception = new ExceptionMessage(ex.InnerException);
        }
        else
        {
            result.Exception = new ExceptionMessage(ex);
        }
    }

    return result;
}


Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
{
    var targetName = new AssemblyName(args.Name);
    foreach (var asm in additionalLoadedAssemblies)
    {
        if (AssemblyName.ReferenceMatchesDefinition(targetName, asm.GetName()))
        {
            return asm;
        }
    }
    return null;
}