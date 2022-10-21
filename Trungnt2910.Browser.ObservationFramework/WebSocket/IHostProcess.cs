namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

interface IHostProcess : IDisposable
{
    void Stop();

    event EventHandler Exited;
}
