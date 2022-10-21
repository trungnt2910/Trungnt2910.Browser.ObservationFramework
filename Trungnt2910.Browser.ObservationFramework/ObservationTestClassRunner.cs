using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestClassRunner : TestClassRunner<ObservationTestCase>
{
    protected IRemoteHost RemoteHost { get; set; }

    readonly int testObjectHandle;

    public ObservationTestClassRunner(int testObjectHandle,
                                      IRemoteHost remoteHost,
                                      ITestClass testClass,
                                      IReflectionTypeInfo @class,
                                      IEnumerable<ObservationTestCase> testCases,
                                      IMessageSink diagnosticMessageSink,
                                      IMessageBus messageBus,
                                      ITestCaseOrderer testCaseOrderer,
                                      ExceptionAggregator aggregator,
                                      CancellationTokenSource cancellationTokenSource)
        : base(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
    {
        this.testObjectHandle = testObjectHandle;
        RemoteHost = remoteHost;
    }

    protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod,
                                                           IReflectionMethodInfo method,
                                                           IEnumerable<ObservationTestCase> testCases,
                                                           object[] constructorArguments)
    {
        return new ObservationTestMethodRunner(testObjectHandle, RemoteHost, testMethod, Class, method, testCases, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();
    }
}
