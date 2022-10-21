using System.Reflection;

namespace Trungnt2910.Browser.ObservationFramework;

public interface IRemoteHost : IDisposable
{
    Task LoadAssembly(string path);
    Task<int> ConstructTestObject(Type type);
    Task DisposeTestObject(int handle);
    Task InvokeMethod(int handle, MethodInfo method);
    string? FrameworkEnvironment { get; }
}
