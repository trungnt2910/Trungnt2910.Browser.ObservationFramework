using System;
using System.Linq;
using System.Threading.Tasks;
using Trungnt2910.Browser;
using Trungnt2910.Browser.ObservationFramework;
using Xunit;

namespace ObservationExample;

public class Given_IntArray : Specification
{
    JsArray<int> _array;

    protected override void EstablishContext()
    {
        _array = JsArray<int>.FromExpression("[1, 2, 3]");
    }

    protected override void DestroyContext()
    {
        _array = null!;
    }

    [Observation]
    public void Should_ReturnCorrectValues()
    {
        Assert.Equal(1, _array[0]);
        Assert.Equal(2, _array[1]);
        Assert.Equal(3, _array[2]);
        Assert.ThrowsAny<Exception>(() => _array[3]);
        Assert.ThrowsAny<Exception>(() => _array[-1]);
    }

    [Observation]
    public void Should_PersistValue()
    {
        Assert.Equal(1, _array[0]);
        _array[0] = 69420;
        Assert.Equal(69420, _array[0]);
        _array[0] = 1;
        Assert.Equal(1, _array[0]);
    }

    [Observation]
    public void Should_BeUsableWithLinq()
    {
        Assert.True(_array.SequenceEqual(new[] { 1, 2, 3 }));
    }
}

public class Given_IntPromise : Specification
{
    Promise<int> _promise1;
    Promise<int> _promise2;

    protected override void EstablishContext()
    {
        _promise1 = Promise<int>.FromExpression("new Promise((resolve, reject) => setTimeout(() => resolve(1), 1000))");
        _promise2 = Promise<int>.FromExpression("new Promise((resolve, reject) => setTimeout(() => resolve(2), 2000))");
    }

    [Observation]
    public async void Should_ReturnCorrectValue()
    {
        Assert.Equal(1, await _promise1);
    }

    [Observation]
    public async void Should_ReturnCorrectValueAgain()
    {
        Assert.Equal(1, await _promise1);
    }

    [Observation]
    public async void Should_BeUsableWithTaskApi()
    {
        var arr = await Task.WhenAll(_promise1, _promise2, Task.FromResult(3));
        Assert.True(arr.SequenceEqual(new[] { 1, 2, 3 }));
    }
}
