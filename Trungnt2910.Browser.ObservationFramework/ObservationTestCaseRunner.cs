using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestCaseRunner : TestCaseRunner<ObservationTestCase>
{
    protected IRemoteHost RemoteHost { get; set; }

    readonly string displayName;
    readonly int testObjectHandle;

    public ObservationTestCaseRunner(int testObjectHandle,
                                     ObservationTestCase testCase,
                                     string displayName,
                                     IRemoteHost remoteHost,
                                     IMessageBus messageBus,
                                     ExceptionAggregator aggregator,
                                     CancellationTokenSource cancellationTokenSource)
        : base(testCase, messageBus, aggregator, cancellationTokenSource)
    {
        this.testObjectHandle = testObjectHandle;
        this.displayName = displayName;

        RemoteHost = remoteHost;
    }

    protected override Task<RunSummary> RunTestAsync()
    {
        var timer = new ExecutionTimer();
        var TestClass = TestCase.TestMethod.TestClass.Class.ToRuntimeType();
        var TestMethod = TestCase.TestMethod.Method.ToRuntimeMethod();
        var test = new ObservationTest(TestCase, displayName);

        return new ObservationTestRunner(testObjectHandle, test, RemoteHost, MessageBus, timer, TestClass, TestMethod, Aggregator, CancellationTokenSource).RunAsync();
    }
}

