using System.Reflection;
using ObservationFramework.WebSocket;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTestInvoker : TestInvoker<ObservationTestCase>
{
    protected IRemoteHost RemoteHost { get; set; }

    private readonly int testObjectHandle;

    public ObservationTestInvoker(int testObjectHandle,
                                  ITest test,
                                  IRemoteHost remoteHost,
                                  IMessageBus messageBus,
                                  Type testClass,
                                  MethodInfo testMethod,
                                  ExceptionAggregator aggregator,
                                  CancellationTokenSource cancellationTokenSource)
        : base(test, messageBus, testClass, null, testMethod, null, aggregator, cancellationTokenSource)
    {
        this.testObjectHandle = testObjectHandle;
        RemoteHost = remoteHost;
    }

    public new Task<decimal> RunAsync()
    {
        return Aggregator.RunAsync(async () =>
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    if (!Aggregator.HasExceptions)
                        await Timer.AggregateAsync(async () =>
                            await RemoteHost.InvokeMethod(testObjectHandle, TestMethod));
                }
            }

            return Timer.Total;
        });
    }
}
