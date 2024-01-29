using Autofac;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Implementations;
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
    public void GetNewIntermediateDataSet_WithOneSubData_ReturnValid()
    {
        var intermediateDataMock = new Mock<IntermediateData>();

        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData()).Returns(intermediateDataMock.Object);

        _intermediateDataManager.RegisterIntermediateData(intermediateDataId,
            intermediateDataRegistryWrapperMock.Object);

        var result = _intermediateDataManager.GetNewIntermediateDataSet();

        result.GetSubData(intermediateDataId).Should().Be(intermediateDataMock.Object);
    }

    [Fact]
    public void GetNewIntermediateDataSet_TwoTimes_ReturnDifferentInstances()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(new Mock<IntermediateData>().Object);

        _intermediateDataManager.RegisterIntermediateData(intermediateDataId,
            intermediateDataRegistryWrapperMock.Object);

        var result1 = _intermediateDataManager.GetNewIntermediateDataSet();
        var result2 = _intermediateDataManager.GetNewIntermediateDataSet();

        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GetNewIntermediateDataSet_Recycled_ReturnSameInstance()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(new Mock<IntermediateData>().Object);

        _intermediateDataManager.RegisterIntermediateData(intermediateDataId,
            intermediateDataRegistryWrapperMock.Object);

        var result1 = _intermediateDataManager.GetNewIntermediateDataSet();
        result1.DecreaseUseCount();
        var result2 = _intermediateDataManager.GetNewIntermediateDataSet();

        result1.Should().Be(result2);
    }

    [Fact]
    public void GetCurrentIntermediateDataSet_AfterSet_ReturnValid()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(new Mock<IntermediateData>().Object);

        _intermediateDataManager.RegisterIntermediateData(intermediateDataId,
            intermediateDataRegistryWrapperMock.Object);

        var intermediateSet = _intermediateDataManager.GetNewIntermediateDataSet();

        _intermediateDataManager.SetCurrentIntermediateDataSet(intermediateSet);

        _intermediateDataManager.GetCurrentIntermediateDataSet().Should().Be(intermediateSet);
    }

    [Fact]
    public void SetCurrentIntermediateDataSet_Multiple_ShouldReusePrevious()
    {
        var intermediateDataRegistryWrapperMock = new Mock<IntermediateDataRegistryWrapper>();
        intermediateDataRegistryWrapperMock.Setup(x => x.CreateIntermediateData())
            .Returns(new Mock<IntermediateData>().Object);

        _intermediateDataManager.RegisterIntermediateData(intermediateDataId,
            intermediateDataRegistryWrapperMock.Object);

        var intermediateSet1 = _intermediateDataManager.GetNewIntermediateDataSet();
        _intermediateDataManager.SetCurrentIntermediateDataSet(intermediateSet1);
        intermediateSet1.DecreaseUseCount();

        var intermediateSet2 = _intermediateDataManager.GetNewIntermediateDataSet();
        _intermediateDataManager.SetCurrentIntermediateDataSet(intermediateSet2);

        var intermediateSet3 = _intermediateDataManager.GetNewIntermediateDataSet();

        intermediateSet1.Should().Be(intermediateSet3);
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