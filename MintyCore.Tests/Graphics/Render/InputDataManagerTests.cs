using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Graphics.Render.Managers.Implementations;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class InputDataManagerTests
{
    private readonly IInputDataManager _inputDataManager = new InputDataManager();
    private readonly Identification _testId = new(1, 2, 3);

    [Fact]
    public void SetKeyIndexedInputData_AfterRegistering_ShouldNotThrow()
    {
        _inputDataManager.RegisterKeyIndexedInputDataType(_testId, new DictionaryInputDataRegistryWrapper<int, int>());

        var act = () => _inputDataManager.SetKeyIndexedInputData(_testId, 1, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetKeyIndexedInputData_WithoutRegisteringType_ShouldThrow()
    {
        var act = () => _inputDataManager.SetKeyIndexedInputData(_testId, 1, 1);
        act.Should().Throw<MintyCoreException>().WithMessage("No dictionary object found for *");
    }

    [Fact]
    public void SetKeyIndexedInputData_WithWrongType_ShouldThrow()
    {
        _inputDataManager.RegisterKeyIndexedInputDataType(_testId, new DictionaryInputDataRegistryWrapper<int, int>());

        var act = () => _inputDataManager.SetKeyIndexedInputData(_testId, 1, "1");
        act.Should().Throw<MintyCoreException>().WithMessage("Type mismatch for *. Expected <*, *> but got <*, *>");
    }

    [Fact]
    public void SetSingletonInputData_AfterRegistering_ShouldNotThrow()
    {
        _inputDataManager.RegisterSingletonInputDataType(_testId, new SingletonInputDataRegistryWrapper<int>());

        var act = () => _inputDataManager.SetSingletonInputData(_testId, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetSingletonInputData_WithoutRegisteringType_ShouldThrow()
    {
        var act = () => _inputDataManager.SetSingletonInputData(_testId, 1);
        act.Should().Throw<MintyCoreException>().WithMessage("Singleton Input Type for * is not registered");
    }

    [Fact]
    public void SetSingletonInputData_WithWrongType_ShouldThrow()
    {
        _inputDataManager.RegisterSingletonInputDataType(_testId, new SingletonInputDataRegistryWrapper<int>());

        var act = () => _inputDataManager.SetSingletonInputData(_testId, "1");

        act.Should().Throw<MintyCoreException>().WithMessage("Wrong type for *, expected * but got *");
    }

    [Fact]
    public void GetSingletonInputData_AfterRegistering_ShouldReturnCorrectType()
    {
        _inputDataManager.RegisterSingletonInputDataType(_testId, new SingletonInputDataRegistryWrapper<int>());

        var act = () => _inputDataManager.GetSingletonInputData<int>(_testId);

        act.Should().NotThrow();
        act().Should().BeOfType<SingletonInputData<int>>();
    }

    [Fact]
    public void GetSingletonInputData_WithoutRegisteringType_ShouldThrow()
    {
        var act = () => _inputDataManager.GetSingletonInputData<int>(_testId);

        act.Should().Throw<MintyCoreException>().WithMessage("Singleton Input Type for * is not registered");
    }

    [Fact]
    public void GetSingletonInputData_WithWrongType_ShouldThrow()
    {
        _inputDataManager.RegisterSingletonInputDataType(_testId, new SingletonInputDataRegistryWrapper<int>());

        var act = () => _inputDataManager.GetSingletonInputData<string>(_testId);

        act.Should().Throw<MintyCoreException>().WithMessage("Wrong type for *, expected * but got *");
    }

    [Fact]
    public void GetDictionaryInputData_AfterRegistering_ShouldReturnCorrectType()
    {
        _inputDataManager.RegisterKeyIndexedInputDataType(_testId, new DictionaryInputDataRegistryWrapper<int, int>());

        var act = () => _inputDataManager.GetDictionaryInputData<int, int>(_testId);

        act.Should().NotThrow();
        act().Should().BeOfType<DictionaryInputData<int, int>>();
    }

    [Fact]
    public void GetDictionaryInputData_WithoutRegisteringType_ShouldThrow()
    {
        var act = () => _inputDataManager.GetDictionaryInputData<int, int>(_testId);

        act.Should().Throw<MintyCoreException>().WithMessage("Dictionary Input Type for * is not registered");
    }

    [Fact]
    public void GetDictionaryInputData_WithWrongKeyType_ShouldThrow()
    {
        _inputDataManager.RegisterKeyIndexedInputDataType(_testId, new DictionaryInputDataRegistryWrapper<int, int>());

        var act = () => _inputDataManager.GetDictionaryInputData<string, int>(_testId);

        act.Should().Throw<MintyCoreException>().WithMessage("Wrong type for *, expected <*, *> but got <*, *>");
    }

    [Fact]
    public void GetDictionaryInputData_WithWrongDataType_ShouldThrow()
    {
        _inputDataManager.RegisterKeyIndexedInputDataType(_testId, new DictionaryInputDataRegistryWrapper<int, int>());

        var act = () => _inputDataManager.GetDictionaryInputData<int, string>(_testId);

        act.Should().Throw<MintyCoreException>().WithMessage("Wrong type for *, expected <*, *> but got <*, *>");
    }
}