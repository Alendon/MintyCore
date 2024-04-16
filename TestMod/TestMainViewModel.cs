using MintyCore;
using MintyCore.Registries;
using MintyCore.UI;
using TestMod.Identifications;

namespace TestMod;

[RegisterViewModel("test_main")]
public class TestMainViewModel(IViewLocator viewLocator) : ViewModelNavigator(viewLocator)
{
    protected override async Task LoadAsync()
    {
        await NavigateTo(ViewIDs.TestMainMenu);
    }

    public override void Quit()
    {
        Engine.ShouldStop = true;
    }
}