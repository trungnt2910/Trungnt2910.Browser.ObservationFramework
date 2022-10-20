using System.Reflection;
using ObservationFramework.WebSocket;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestCollectionRunner : TestCollectionRunner<ObservationTestCase>
{
    readonly IMessageSink diagnosticMessageSink;

    public ObservationTestCollectionRunner(ITestCollection testCollection,
                                           IEnumerable<ObservationTestCase> testCases,
                                           IMessageSink diagnosticMessageSink,
                                           IMessageBus messageBus,
                                           ITestCaseOrderer testCaseOrderer,
                                           ExceptionAggregator aggregator,
                                           CancellationTokenSource cancellationTokenSource)
        : base(testCollection, testCases, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
    {
        this.diagnosticMessageSink = diagnosticMessageSink;
    }

    protected override async Task<RunSummary> RunTestClassAsync(ITestClass testClass,
                                                                IReflectionTypeInfo @class,
                                                                IEnumerable<ObservationTestCase> testCases)
    {
        var timer = new ExecutionTimer();
        int testObjectHandle = 0;

        await Aggregator.RunAsync(async () => testObjectHandle = await WebSocketHost.ConstructTestObjectOnHost(testClass.Class.ToRuntimeType()));

        if (Aggregator.HasExceptions)
            return FailEntireClass(testCases, timer);

        var result = await new ObservationTestClassRunner(testObjectHandle, testClass, @class, testCases, diagnosticMessageSink, MessageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();

        await Aggregator.RunAsync(async () => await WebSocketHost.DisposeTestObjectOnHost(testObjectHandle));

        return result;
    }

    private RunSummary FailEntireClass(IEnumerable<ObservationTestCase> testCases, ExecutionTimer timer)
    {
        foreach (var testCase in testCases)
        {
            MessageBus.QueueMessage(new TestFailed(new ObservationTest(testCase, testCase.DisplayName), timer.Total,
                "Exception was thrown in class constructor", Aggregator.ToException()));
        }
        int count = testCases.Count();
        return new RunSummary { Failed = count, Total = count };
    }
}
