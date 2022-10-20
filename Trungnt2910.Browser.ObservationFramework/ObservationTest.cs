using Xunit;
using Xunit.Abstractions;

namespace Trungnt2910.Browser.ObservationFramework;

public class ObservationTest : LongLivedMarshalByRefObject, ITest
{
    public ObservationTest(ObservationTestCase testCase, string displayName)
    {
        TestCase = testCase;
        DisplayName = displayName;
    }

    public string DisplayName { get; private set; }

    public ObservationTestCase TestCase { get; private set; }

    ITestCase ITest.TestCase { get { return TestCase; } }
}
