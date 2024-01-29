using Autofac;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Implementations;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class IntermediateManagerTests : IDisposable
{
    private readonly IIntermediateManager _intermediateManager;
    private readonly IContainer _container;
    private readonly Mock<IInputManager> _inputManagerMock;

    private readonly Identification inputModuleId;
    private readonly Identification intermediateDataId;

    public IntermediateManagerTests()
    {
        _inputManagerMock = new Mock<IInputManager>();
        
        var builder = new ContainerBuilder();
        builder.RegisterInstance(_inputManagerMock.Object).As<IInputManager>();
        builder.RegisterType<IntermediateManager>().As<IIntermediateManager>();

        _container = builder.Build();
        _intermediateManager = _container.Resolve<IIntermediateManager>();

        inputModuleId = new Identification(1, 2, 3);
        intermediateDataId = new Identification(4, 5, 6);
    }

    public void Dispose()
    {
        _container.Dispose();
    }

    [Fact]
    public void GetNewIntermediateDataSet_WithOneSubData_ReturnValid()
    {
        var intermediateDataMock = new Mock<IntermediateData>();

        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData()).Returns(intermediateDataMock.Object);
        
        _intermediateManager.RegisterIntermediateData(intermediateDataId, intermediateDataRegistryWrapperMock.Object);
        
        var result = _intermediateManager.GetNewIntermediateDataSet();
        
        result.GetSubData(intermediateDataId).Should().Be(intermediateDataMock.Object);
    }
    
    [Fact]
    public void GetNewIntermediateDataSet_TwoTimes_ReturnDifferentInstances()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData()).Returns(new Mock<IntermediateData>().Object);
        
        _intermediateManager.RegisterIntermediateData(intermediateDataId, intermediateDataRegistryWrapperMock.Object);
        
        var result1 = _intermediateManager.GetNewIntermediateDataSet();
        var result2 = _intermediateManager.GetNewIntermediateDataSet();
        
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GetNewIntermediateDataSet_Recycled_ReturnSameInstance()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData()).Returns(new Mock<IntermediateData>().Object);
        
        _intermediateManager.RegisterIntermediateData(intermediateDataId, intermediateDataRegistryWrapperMock.Object);
        
        var result1 = _intermediateManager.GetNewIntermediateDataSet();
        result1.DecreaseUseCount();
        var result2 = _intermediateManager.GetNewIntermediateDataSet();
        
        result1.Should().Be(result2);
    }

    [Fact]
    public void GetCurrentIntermediateDataSet_AfterSet_ReturnValid()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData()).Returns(new Mock<IntermediateData>().Object);
        
        _intermediateManager.RegisterIntermediateData(intermediateDataId, intermediateDataRegistryWrapperMock.Object);
        
        var intermediateSet = _intermediateManager.GetNewIntermediateDataSet();
        
        _intermediateManager.SetCurrentIntermediateDataSet(intermediateSet);
        
        _intermediateManager.GetCurrentIntermediateDataSet().Should().Be(intermediateSet);
    }

    [Fact]
    public void SetCurrentIntermediateDataSet_Multiple_ShouldReusePrevious()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData()).Returns(new Mock<IntermediateData>().Object);
        
        _intermediateManager.RegisterIntermediateData(intermediateDataId, intermediateDataRegistryWrapperMock.Object);
        
        var intermediateSet1 = _intermediateManager.GetNewIntermediateDataSet();
        _intermediateManager.SetCurrentIntermediateDataSet(intermediateSet1);
        intermediateSet1.DecreaseUseCount();
        
        var intermediateSet2 = _intermediateManager.GetNewIntermediateDataSet();
        _intermediateManager.SetCurrentIntermediateDataSet(intermediateSet2);
        
        var intermediateSet3 = _intermediateManager.GetNewIntermediateDataSet();
        
        intermediateSet1.Should().Be(intermediateSet3);
    }

    [Fact]
    public void ValidateIntermediateDataProvided_NoConsumerNoProvider_ShouldNotThrow()
    {
        throw new NotImplementedException();
    }
    
    [Fact]
    public void ValidateIntermediateDataProvided_NoConsumerWithProvider_ShouldNotThrow()
    {
        throw new NotImplementedException();
    }
    
    [Fact]
    public void ValidateIntermediateDataProvided_ConsumerWithProvider_ShouldNotThrow()
    {
        throw new NotImplementedException();
    }
    
    [Fact]
    public void ValidateIntermediateDataProvided_ConsumerWithMissingProvider_ShouldThrow()
    {
        throw new NotImplementedException();
    }
}