using Autofac;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Graphics.Render.Managers.Implementations;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class IntermediateDataManagerTests : IDisposable
{
    private readonly IIntermediateDataManager _intermediateDataManager;
    private readonly IContainer _container;

    private readonly Identification _intermediateDataId;

    public IntermediateDataManagerTests()
    {
        Mock<IInputModuleManager> inputModuleManagerMock = new();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(inputModuleManagerMock.Object).As<IInputModuleManager>();
        builder.RegisterType<IntermediateDataManager>().As<IIntermediateDataManager>();

        _container = builder.Build();
        _intermediateDataManager = _container.Resolve<IIntermediateDataManager>();

        _intermediateDataId = new Identification(1, 2, 3);
    }

    public void Dispose()
    {
        _container.Dispose();
    }

    [Fact]
    public void GetNewIntermediateData_MultipleWithoutReturn_AlwaysNew()
    {
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData(It.IsAny<IIntermediateDataManager>()))
            .Returns((IIntermediateDataManager _) => new Mock<IntermediateData>().Object);
        
        _intermediateDataManager.RegisterIntermediateData(_intermediateDataId, intermediateWrapperMock.Object);

        var result1 = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        var result2 = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GetNewIntermediateData_ShouldCopyCurrentData()
    {
        var firstMock = new Mock<IntermediateData>();
        var secondMock = new Mock<IntermediateData>();
        var first = true;
        
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData(It.IsAny<IIntermediateDataManager>()))
            // ReSharper disable once AccessToModifiedClosure
            // For testing purposes this is the easiest way to achieve the desired behavior
            .Returns(() => first ? firstMock.Object : secondMock.Object);
        
        _intermediateDataManager.RegisterIntermediateData(_intermediateDataId, intermediateWrapperMock.Object);

        var originalData = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        _intermediateDataManager.SetCurrentData(_intermediateDataId, originalData);
        
        first = false;
        var expectCopied = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        
        originalData.Should().NotBe(expectCopied);
        secondMock.Verify(x => x.CopyFrom(originalData), Times.Once);
    }

    [Fact]
    public void GetNewIntermediateData_MultipleWithRecycle_ShouldReturnOldInstance()
    {
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData(It.IsAny<IIntermediateDataManager>()))
            .Returns((IIntermediateDataManager _) => new Mock<IntermediateData>().Object);
        
        _intermediateDataManager.RegisterIntermediateData(_intermediateDataId, intermediateWrapperMock.Object);
        
        var result1 = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        _intermediateDataManager.RecycleIntermediateData(_intermediateDataId, result1);
        var result2 = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        
        result1.Should().Be(result2);
    }

    [Fact]
    public void RecycleIntermediateData_ClearCalledOnData()
    {
        var intermediateMock = new Mock<IntermediateData>();
        
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData(It.IsAny<IIntermediateDataManager>()))
            .Returns(intermediateMock.Object);
        
        _intermediateDataManager.RegisterIntermediateData(_intermediateDataId, intermediateWrapperMock.Object);
        
        var result1 = _intermediateDataManager.GetNewIntermediateData(_intermediateDataId);
        _intermediateDataManager.RecycleIntermediateData(_intermediateDataId, result1);
        
        intermediateMock.Verify(x => x.Clear(), Times.Once);
    }
}