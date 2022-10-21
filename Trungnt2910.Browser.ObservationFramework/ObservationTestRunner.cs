using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestRunner : TestRunner<ObservationTestCase>
{
    protected IRemoteHost RemoteHost { get; set; }

    readonly int testObjectHandle;
    readonly ExecutionTimer timer;
    
    public ObservationTestRunner(int testObjectHandle,
                                 ITest test,
                                 IRemoteHost remoteHost,
                                 IMessageBus messageBus,
                                 ExecutionTimer timer,
                                 Type testClass,
                                 MethodInfo testMethod,
                                 ExceptionAggregator aggregator,
                                 CancellationTokenSource cancellationTokenSource)
        : base(test, messageBus, testClass, null, testMethod, null, null, aggregator, cancellationTokenSource)
    {
        this.testObjectHandle = testObjectHandle;
        this.timer = timer;

        RemoteHost = remoteHost;
    }

    protected override async Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
    {
        var duration = await new ObservationTestInvoker(testObjectHandle, Test, RemoteHost, MessageBus, TestClass, TestMethod, aggregator, CancellationTokenSource).RunAsync();
        return Tuple.Create(duration, string.Empty);
    }
}

