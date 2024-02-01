using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class ModuleDataAccessorTests
{
    private readonly ModuleDataAccessor _moduleDataAccessor;

    private readonly Mock<IInputDataManager> _inputDataManagerMock;
    private readonly Mock<IIntermediateDataManager> _intermediateDataManagerMock;

    private static readonly Identification _inputModuleId = new(1, 2, 3);
    private static readonly Identification _renderModuleId = new(4, 5, 6);

    private static readonly Identification _inputDataId = new(7, 8, 9);
    private static readonly Identification _intermediateDataId = new(10, 11, 12);

    public ModuleDataAccessorTests()
    {
        _inputDataManagerMock = new Mock<IInputDataManager>();
        _intermediateDataManagerMock = new Mock<IIntermediateDataManager>();
        _moduleDataAccessor = new ModuleDataAccessor(_inputDataManagerMock.Object, _intermediateDataManagerMock.Object);
    }

    [Fact]
    public void SetInputDataConsumer_Valid_ShouldNotThrow()
    {
        _inputDataManagerMock.Setup(x => x.GetRegisteredInputDataIds()).Returns(new[] { _inputDataId });
        var act = void () =>
            _moduleDataAccessor.SetInputDataConsumer(_inputDataId, _inputModuleId);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetInputDataConsumer_NonExistingInputData_ShouldThrow()
    {
        var act = void () =>
            _moduleDataAccessor.SetInputDataConsumer(_inputDataId, _inputModuleId);

        act.Should().Throw<MintyCoreException>().WithMessage("Input data * does not exist.");
    }

    [Fact]
    public void SetIntermediateDataConsumer_Valid_ShouldNotThrow()
    {
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { _intermediateDataId });

        var act = void () =>
            _moduleDataAccessor.SetIntermediateDataConsumer(_intermediateDataId, _inputModuleId);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetIntermediateDataConsumer_NonExistingIntermediateData_ShouldThrow()
    {
        var act = void () =>
            _moduleDataAccessor.SetIntermediateDataConsumer(_intermediateDataId, _inputModuleId);

        act.Should().Throw<MintyCoreException>().WithMessage("Intermediate data * does not exist.");
    }

    [Fact]
    public void SetIntermediateDataProvider_Valid_ShouldNotThrow()
    {
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { _intermediateDataId });

        var act = void () =>
            _moduleDataAccessor.SetIntermediateDataProvider(_intermediateDataId, _inputModuleId);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetIntermediateDataProvider_NonExistingIntermediateData_ShouldThrow()
    {
        var act = void () =>
            _moduleDataAccessor.SetIntermediateDataProvider(_intermediateDataId, _inputModuleId);

        act.Should().Throw<MintyCoreException>().WithMessage("Intermediate data * does not exist.");
    }

    [Fact]
    public void SetIntermediateDataProvider_MultipleProviders_ShouldThrow()
    {
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { _intermediateDataId });
        _moduleDataAccessor.SetIntermediateDataProvider(_intermediateDataId, _inputModuleId);

        var act = void () =>
            _moduleDataAccessor.SetIntermediateDataProvider(_intermediateDataId, _renderModuleId);

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
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { _intermediateDataId });
        _moduleDataAccessor.SetIntermediateDataProvider(_intermediateDataId, _inputModuleId);

        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_ProviderWithConsumer_ShouldNotThrow()
    {
        var secondInputModuleId = new Identification(13, 14, 15);

        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { _intermediateDataId });
        _moduleDataAccessor.SetIntermediateDataProvider(_intermediateDataId, _inputModuleId);
        _moduleDataAccessor.SetIntermediateDataConsumer(_intermediateDataId, secondInputModuleId);

        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateIntermediateDataProvided_NoProviderWithConsumer_ShouldThrow()
    {
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { _intermediateDataId });
        _moduleDataAccessor.SetIntermediateDataConsumer(_intermediateDataId, _inputModuleId);

        var act = void () =>
            _moduleDataAccessor.ValidateIntermediateDataProvided();

        act.Should().Throw<MintyCoreException>()
            .WithMessage("No intermediate data provider found for * (consumers: *)");
    }

    [Fact]
    public void GetSingletonInputData_ShouldReturnDataFromManager()
    {
        var inputData = new SingletonInputData<int>();
        _inputDataManagerMock.Setup(x => x.GetSingletonInputData<int>(_inputDataId)).Returns(inputData);
        _inputDataManagerMock.Setup(x => x.GetRegisteredInputDataIds()).Returns(new[] { _inputDataId });
        _moduleDataAccessor.SetInputDataConsumer(_inputDataId, _inputModuleId);

        var result = _moduleDataAccessor.GetSingletonInputData<int>(_inputDataId, _inputModuleId);

        result.Should().Be(inputData);
    }

    [Fact]
    public void GetSingletonInputData_NotSetAsConsumer_ShouldThrow()
    {
        var inputData = new SingletonInputData<int>();
        _inputDataManagerMock.Setup(x => x.GetSingletonInputData<int>(_inputDataId)).Returns(inputData);

        var act = void () =>
            _moduleDataAccessor.GetSingletonInputData<int>(_inputDataId, _inputModuleId);

        act.Should().Throw<MintyCoreException>().WithMessage("Input module * is not set as consumer for *");
    }

    [Fact]
    public void SortInputModules_TwoModulesNoCrossDependency_ShouldReturnBothInAnyOrder()
    {
        //use another set of ids, as this test requires multiple and to avoid confusion with other tests
        var inputModule1 = new Identification(1, 2, 3);
        var inputModule2 = new Identification(4, 5, 6);

        var intermediate1 = new Identification(7, 8, 9);
        var intermediate2 = new Identification(10, 11, 12);

        var inputData = new Identification(13, 14, 15);

        //"register" the data ids
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { intermediate1, intermediate2 });
        _inputDataManagerMock.Setup(x => x.GetRegisteredInputDataIds()).Returns(new[] { inputData });

        //set up the dependencies
        _moduleDataAccessor.SetIntermediateDataProvider(intermediate1, inputModule1);
        _moduleDataAccessor.SetIntermediateDataProvider(intermediate2, inputModule2);

        _moduleDataAccessor.SetInputDataConsumer(inputData, inputModule1);
        _moduleDataAccessor.SetInputDataConsumer(inputData, inputModule2);

        var result = _moduleDataAccessor.SortInputModules([inputModule1, inputModule2]);

        result.Should().BeEquivalentTo([inputModule1, inputModule2], options => options.WithoutStrictOrdering());
    }

    [Fact]
    public void SortInputModules_ThreeModulesWithDependency_ShouldReturnSorted()
    {
        var inputModule1 = new Identification(1, 2, 3);
        var inputModule2 = new Identification(4, 5, 6);
        var inputModule3 = new Identification(7, 8, 9);

        var intermediate1 = new Identification(10, 11, 12);
        var intermediate2 = new Identification(13, 14, 15);

        var inputData = new Identification(16, 17, 18);


        //"register" the data ids
        _intermediateDataManagerMock.Setup(x => x.GetRegisteredIntermediateDataIds())
            .Returns(new[] { intermediate1, intermediate2 });
        _inputDataManagerMock.Setup(x => x.GetRegisteredInputDataIds()).Returns(new[] { inputData });

        //set up the dependencies
        _moduleDataAccessor.SetInputDataConsumer(inputData, inputModule1);
        _moduleDataAccessor.SetIntermediateDataProvider(intermediate1, inputModule1);
        _moduleDataAccessor.SetIntermediateDataConsumer(intermediate1, inputModule2);
        _moduleDataAccessor.SetIntermediateDataProvider(intermediate2, inputModule2);
        _moduleDataAccessor.SetIntermediateDataConsumer(intermediate2, inputModule3);

        var result = _moduleDataAccessor.SortInputModules([inputModule1, inputModule2, inputModule3]);

        result.Should().BeEquivalentTo([inputModule1, inputModule2, inputModule3],
            options => options.WithStrictOrdering());
    }
}