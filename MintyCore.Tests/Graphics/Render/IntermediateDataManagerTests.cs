using Autofac;
using MintyCore.Graphics.Render;
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
    private readonly Mock<IInputModuleManager> _inputModuleManagerMock;

    private readonly Identification intermediateDataId;

    public IntermediateDataManagerTests()
    {
        _inputModuleManagerMock = new Mock<IInputModuleManager>();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(_inputModuleManagerMock.Object).As<IInputModuleManager>();
        builder.RegisterType<IntermediateDataManager>().As<IIntermediateDataManager>();

        _container = builder.Build();
        _intermediateDataManager = _container.Resolve<IIntermediateDataManager>();

        intermediateDataId = new Identification(1, 2, 3);
    }

    public void Dispose()
    {
        _container.Dispose();
    }

    [Fact]
    public void GetNewIntermediateData_MultipleWithoutReturn_AlwaysNew()
    {
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(() => new Mock<IntermediateData>().Object);
        
        _intermediateDataManager.RegisterIntermediateData(intermediateDataId, intermediateWrapperMock.Object);

        var result1 = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        var result2 = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GetNewIntermediateData_ShouldCopyCurrentData()
    {
        var firstMock = new Mock<IntermediateData>();
        var secondMock = new Mock<IntermediateData>();
        var first = true;
        
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData())
            // ReSharper disable once AccessToModifiedClosure
            // For testing purposes this is the easiest way to achieve the desired behavior
            .Returns(() => first ? firstMock.Object : secondMock.Object);
        
        _intermediateDataManager.RegisterIntermediateData(intermediateDataId, intermediateWrapperMock.Object);

        var originalData = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        _intermediateDataManager.SetCurrentData(intermediateDataId, originalData);
        
        first = false;
        var expectCopied = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        
        originalData.Should().NotBe(expectCopied);
        secondMock.Verify(x => x.CopyFrom(originalData), Times.Once);
    }

    [Fact]
    public void GetNewIntermediateData_MultipleWithRecycle_ShouldReturnOldInstance()
    {
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(() => new Mock<IntermediateData>().Object);
        
        _intermediateDataManager.RegisterIntermediateData(intermediateDataId, intermediateWrapperMock.Object);
        
        var result1 = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        _intermediateDataManager.RecycleIntermediateData(intermediateDataId, result1);
        var result2 = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        
        result1.Should().Be(result2);
    }

    [Fact]
    public void RecycleIntermediateData_ResetCalledOnData()
    {
        var intermediateMock = new Mock<IntermediateData>();
        
        var intermediateWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(intermediateMock.Object);
        
        _intermediateDataManager.RegisterIntermediateData(intermediateDataId, intermediateWrapperMock.Object);
        
        var result1 = _intermediateDataManager.GetNewIntermediateData(intermediateDataId);
        _intermediateDataManager.RecycleIntermediateData(intermediateDataId, result1);
        
        intermediateMock.Verify(x => x.Reset(), Times.Once);
    }

    [Fact]
    public void GetCurrentIntermediateDataSet_AfterSet_ReturnValid()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void SetCurrentIntermediateDataSet_Multiple_ShouldReusePrevious()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_NoConsumerNoProvider_ShouldNotThrow()
    {
        var act = () => _intermediateDataManager.ValidateIntermediateDataProvided();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_NoConsumerWithProvider_ShouldNotThrow()
    {
        var providerId = new Identification(1, 2, 3);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([providerId]));

        _intermediateDataManager.SetIntermediateProvider(providerId, intermediateDataId);

        var act = () => _intermediateDataManager.ValidateIntermediateDataProvided();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_ConsumerWithProvider_ShouldNotThrow()
    {
        var providerId = new Identification(1, 2, 3);
        var consumerId = new Identification(1, 2, 4);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([providerId, consumerId]));

        _intermediateDataManager.SetIntermediateProvider(providerId, intermediateDataId);
        _intermediateDataManager.SetIntermediateConsumerInputModule(consumerId, intermediateDataId);

        var act = () => _intermediateDataManager.ValidateIntermediateDataProvided();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_ConsumerWithMissingProvider_ShouldThrow()
    {
        var providerId = new Identification(1, 2, 3);
        var consumerId = new Identification(1, 2, 4);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([providerId, consumerId]));

        _intermediateDataManager.SetIntermediateConsumerInputModule(consumerId, intermediateDataId);

        var act = () => _intermediateDataManager.ValidateIntermediateDataProvided();
        act.Should().Throw<MintyCoreException>()
            .WithMessage("No intermediate data provider found for * (consumers: *)");
    }

    [Fact]
    public void SetIntermediateConsumerInputModule_ConsumerNotRegistered_ShouldThrow()
    {
        var providerId = new Identification(1, 2, 3);
        var consumerId = new Identification(1, 2, 4);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([providerId]));

        var act = () => _intermediateDataManager.SetIntermediateConsumerInputModule(consumerId, intermediateDataId);
        act.Should().Throw<MintyCoreException>().WithMessage("Input Module * is not registered");
    }

    [Fact]
    public void SetIntermediateConsumerInputModule_ConsumerRegistered_ShouldNotThrow()
    {
        var providerId = new Identification(1, 2, 3);
        var consumerId = new Identification(1, 2, 4);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([providerId, consumerId]));

        var act = () => _intermediateDataManager.SetIntermediateConsumerInputModule(consumerId, intermediateDataId);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetIntermediateProvider_ProviderNotRegistered_ShouldThrow()
    {
        var providerId = new Identification(1, 2, 3);
        var consumerId = new Identification(1, 2, 4);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([consumerId]));

        var act = () => _intermediateDataManager.SetIntermediateProvider(providerId, intermediateDataId);
        act.Should().Throw<MintyCoreException>().WithMessage("Input Module * is not registered");
    }

    [Fact]
    public void SetIntermediateProvider_ProviderRegistered_ShouldNotThrow()
    {
        var providerId = new Identification(1, 2, 3);
        var consumerId = new Identification(1, 2, 4);
        _inputModuleManagerMock.SetupGet(x => x.RegisteredInputModuleIds)
            .Returns(new HashSet<Identification>([providerId, consumerId]));

        var act = () => _intermediateDataManager.SetIntermediateProvider(providerId, intermediateDataId);
        act.Should().NotThrow();
    }
}