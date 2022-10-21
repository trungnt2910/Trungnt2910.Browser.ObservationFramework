using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestCase : TestMethodTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ObservationTestCase() { }

    public ObservationTestCase(TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod)
        : base(defaultMethodDisplay, defaultMethodDisplayOptions, testMethod) { }

    protected override void Initialize()
    {
        base.Initialize();

        DisplayName = TestMethod.Method.Name;
    }

    public Task<RunSummary> RunAsync(int testObjectHandle,
                                     IRemoteHost remoteHost,
                                     IMessageBus messageBus,
                                     ExceptionAggregator aggregator,
                                     CancellationTokenSource cancellationTokenSource)
    {
        return new ObservationTestCaseRunner(testObjectHandle, this, DisplayName, remoteHost, messageBus, aggregator, cancellationTokenSource).RunAsync();
    }
}