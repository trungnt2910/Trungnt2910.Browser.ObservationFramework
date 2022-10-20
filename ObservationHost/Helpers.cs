using System.Reflection;
using System.Security;
using Xunit.Sdk;

namespace ObservationHost;

internal static class Helpers
{
    public static Assembly GetAssemblyByName(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                .First(assembly => AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), new AssemblyName(name)));
    }

    public static async Task InvokeMethod(object testClassInstance, MethodInfo method)
    {
        var oldSyncContext = SynchronizationContext.Current!;

        try
        {
            var asyncSyncContext = new AsyncTestSyncContext(oldSyncContext);
            SetSynchronizationContext(asyncSyncContext);

            var result = method.Invoke(testClassInstance, null);
            var task = GetTaskFromResult(result);
            if (task != null)
            {
                if (task.Status == TaskStatus.Created)
                    throw new InvalidOperationException("Test method returned a non-started Task (tasks must be started before being returned)");
                await task;
            }
            else
            {
                var ex = await asyncSyncContext.WaitForCompletionAsync();
                if (ex != null)
                {
                    throw ex;
                }
            }
        }
        finally
        {
            SetSynchronizationContext(oldSyncContext);
        }
    }

    [SecuritySafeCritical]
    static void SetSynchronizationContext(SynchronizationContext context)
        => SynchronizationContext.SetSynchronizationContext(context);

    /// <summary>
    /// Given an object, will determine if it is an instance of <see cref="Task"/> (in which case, it is
    /// directly returned), or an instance of <see cref="T:Microsoft.FSharp.Control.FSharpAsync`1"/>
    /// (in which case it is converted), or neither (in which case <c>null</c> is returned).
    /// </summary>
    /// <param name="obj">The object to convert</param>
    public static Task? GetTaskFromResult(object? obj)
    {
        if (obj == null)
            return null;

        var task = obj as Task;
        if (task != null)
            return task;

        var type = obj.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == "Microsoft.FSharp.Control.FSharpAsync`1")
        {
            var startAsTaskOpenGenericMethod = type.Assembly.GetType("Microsoft.FSharp.Control.FSharpAsync")!
                                                                 .GetRuntimeMethods()
                                                                 .FirstOrDefault(m => m.Name == "StartAsTask")!;

            return startAsTaskOpenGenericMethod.MakeGenericMethod(type.GetGenericArguments()[0])
                                               .Invoke(null, new[] { obj, null, null }) as Task;
        }

        return null;
    }
}
