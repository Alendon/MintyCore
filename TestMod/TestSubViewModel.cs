using MintyCore.Registries;
using MintyCore.UI;

namespace TestMod;

[RegisterViewModel("test_sub")]
public class TestSubViewModel : ViewModel
{
    protected override Task LoadAsync()
    {
        return Task.CompletedTask;
    }
}