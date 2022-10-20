using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestClassRunner : TestClassRunner<ObservationTestCase>
{
    readonly int testObjectHandle;

    public ObservationTestClassRunner(int testObjectHandle,
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
    }

    protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod,
                                                           IReflectionMethodInfo method,
                                                           IEnumerable<ObservationTestCase> testCases,
                                                           object[] constructorArguments)
    {
        return new ObservationTestMethodRunner(testObjectHandle, testMethod, Class, method, testCases, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();
    }
}
