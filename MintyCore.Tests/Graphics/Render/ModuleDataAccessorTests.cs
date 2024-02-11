using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Graphics.Render.Managers.Implementations;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class ModuleDataAccessorTests
{
    private readonly ModuleDataAccessor _moduleDataAccessor;

    private readonly Mock<IInputDataManager> _inputDataManagerMock;

    private static readonly Identification _inputModuleId = new(1, 2, 3);

    private static readonly Identification _inputDataId = new(7, 8, 9);
    private static readonly Identification _intermediateDataId = new(10, 11, 12);
    
    private readonly Mock<InputModule> _inputModuleMock;

    public ModuleDataAccessorTests()
    {
        _inputDataManagerMock = new Mock<IInputDataManager>();
        var intermediateDataManager = new IntermediateDataManager();
        var renderDataManagerMock = new Mock<IRenderDataManager>();
        _moduleDataAccessor = new ModuleDataAccessor(_inputDataManagerMock.Object, intermediateDataManager, renderDataManagerMock.Object);
        
        _inputModuleMock = new Mock<InputModule>();
        _inputModuleMock.Setup(x => x.Identification).Returns(_inputModuleId);
        
        intermediateDataManager.RegisterIntermediateData<IntermediateDataMock>(_intermediateDataId);
    }

    [Fact]
    public void SetInputDataConsumer_Valid_ShouldNotThrow()
    {
        _inputDataManagerMock.Setup(x => x.GetRegisteredInputDataIds()).Returns(new[] { _inputDataId });
        var act = void () =>
            _moduleDataAccessor.UseSingletonInputData<int>(_inputDataId, _inputModuleMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetIntermediateDataConsumer_Valid_ShouldNotThrow()
    {
        var act = void () =>
            _moduleDataAccessor.UseIntermediateData<IntermediateDataMock>(_intermediateDataId, _inputModuleMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetIntermediateDataProvider_Valid_ShouldNotThrow()
    {
     var act = void () =>
            _moduleDataAccessor.ProvideIntermediateData<IntermediateDataMock>(_intermediateDataId, _inputModuleMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetIntermediateDataProvider_MultipleProviders_ShouldThrow()
    {
        
        _moduleDataAccessor.ProvideIntermediateData<IntermediateDataMock>(_intermediateDataId, _inputModuleMock.Object);

        var secondInputModuleMock = new Mock<InputModule>();
        secondInputModuleMock.Setup(x => x.Identification).Returns(new Identification(13, 14, 15));
        
        var act = void () =>
            _moduleDataAccessor.ProvideIntermediateData<IntermediateDataMock>(_intermediateDataId, secondInputModuleMock.Object);

        act.Should().Throw<MintyCoreException>()
            .WithMessage("Intermediate data * already has a provider. (Current: *, New: *)");
    }

    [Fact]
    public void ValidateIntermediateDataProvided_NoProviderNoConsumer_ShouldNotThrow()
    {
        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_ProviderWithNoConsumer_ShouldNotThrow()
    {
        
        _moduleDataAccessor.ProvideIntermediateData<IntermediateDataMock>(_intermediateDataId, _inputModuleMock.Object);

        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_ProviderWithConsumer_ShouldNotThrow()
    {
        var secondInputModuleMock = new Mock<InputModule>();
        secondInputModuleMock.Setup(x => x.Identification).Returns(new Identification(13, 14, 15));

        _moduleDataAccessor.ProvideIntermediateData<IntermediateDataMock>(_intermediateDataId, _inputModuleMock.Object);
        _moduleDataAccessor.UseIntermediateData<IntermediateDataMock>(_intermediateDataId, secondInputModuleMock.Object);

        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_NoProviderWithConsumer_ShouldThrow()
    {
        _moduleDataAccessor.UseIntermediateData<IntermediateDataMock>(_intermediateDataId, _inputModuleMock.Object);

        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().Throw<MintyCoreException>()
            .WithMessage("No intermediate data provider found for * (consumers: *)");
    }

    public class IntermediateDataMock : IntermediateData
    {
        public override void Clear()
        {
        }

        public override Identification Identification => _intermediateDataId;
        public override void Dispose()
        {
        }
    }
}