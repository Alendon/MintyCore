using MintyCore.Utils;
using Serilog;

namespace TestMod;

[Singleton<ITestDependency>]
public class TestDependency : ITestDependency
{
    public void DoSomething()
    {
        Log.Information("TestDependency did something!");
    }
}

public interface ITestDependency
{
    public void DoSomething();
}