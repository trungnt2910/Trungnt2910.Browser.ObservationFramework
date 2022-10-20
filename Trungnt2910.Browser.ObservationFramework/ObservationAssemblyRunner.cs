using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationAssemblyRunner : TestAssemblyRunner<ObservationTestCase>
{
    public ObservationAssemblyRunner(ITestAssembly testAssembly,
                                     IEnumerable<ObservationTestCase> testCases,
                                     IMessageSink diagnosticMessageSink,
                                     IMessageSink executionMessageSink,
                                     ITestFrameworkExecutionOptions executionOptions)
        : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
    {
        TestCaseOrderer = new ObservationTestCaseOrderer();
    }

    protected override string GetTestFrameworkDisplayName()
    {
        return "Observation Framework";
    }

    protected override string GetTestFrameworkEnvironment()
    {
        return string.Format("{0}-bit .NET {1}", IntPtr.Size * 8, Environment.Version);
    }

    protected override async Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus,
                                                               ITestCollection testCollection,
                                                               IEnumerable<ObservationTestCase> testCases,
                                                               CancellationTokenSource cancellationTokenSource)
    {
        return await new ObservationTestCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
    }
}
