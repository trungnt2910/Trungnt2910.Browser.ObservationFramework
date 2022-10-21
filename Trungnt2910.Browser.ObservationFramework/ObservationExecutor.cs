using System.Reflection;
using Trungnt2910.Browser.ObservationFramework.WebSocket;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationExecutor : TestFrameworkExecutor<ObservationTestCase>
{
    public ObservationExecutor(AssemblyName assemblyName,
                               ISourceInformationProvider sourceInformationProvider,
                               IMessageSink diagnosticMessageSink)
        : base(assemblyName, sourceInformationProvider, diagnosticMessageSink) { }

    protected override ITestFrameworkDiscoverer CreateDiscoverer()
    {
        return new ObservationDiscoverer(AssemblyInfo, SourceInformationProvider, DiagnosticMessageSink);
    }

    protected override async void RunTestCases(IEnumerable<ObservationTestCase> testCases,
                                               IMessageSink executionMessageSink,
                                               ITestFrameworkExecutionOptions executionOptions)
    {
        using IRemoteHost remoteHost = await WebSocketRemoteHost.CreateAsync(DiagnosticMessageSink, executionMessageSink);
        await remoteHost.LoadAssembly(Assembly.GetExecutingAssembly().Location);
        await remoteHost.LoadAssembly(AssemblyInfo.AssemblyPath);

        var testAssembly = new TestAssembly(AssemblyInfo);

        using (var assemblyRunner = new ObservationAssemblyRunner(remoteHost, testAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
            await assemblyRunner.RunAsync();
    }
}
