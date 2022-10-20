using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestMethodRunner : TestMethodRunner<ObservationTestCase>
{
    readonly int testObjectHandle;

    public ObservationTestMethodRunner(int testObjectHandle,
                                       ITestMethod testMethod,
                                       IReflectionTypeInfo @class,
                                       IReflectionMethodInfo method,
                                       IEnumerable<ObservationTestCase> testCases,
                                       IMessageBus messageBus,
                                       ExceptionAggregator aggregator,
                                       CancellationTokenSource cancellationTokenSource)
        : base(testMethod, @class, method, testCases, messageBus, aggregator, cancellationTokenSource)
    {
        this.testObjectHandle = testObjectHandle;
    }

    protected override Task<RunSummary> RunTestCaseAsync(ObservationTestCase testCase)
    {
        return testCase.RunAsync(testObjectHandle, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource);
    }
}
