using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MintyCore.Registries;
using MintyCore.UI;
using TestMod.Identifications;

namespace TestMod;

[RegisterViewModel("test_main_menu")]
public partial class TestMainMenuViewModel : ViewModel
{
    [RelayCommand]
    private async Task SubView()
    {
        await Navigator.NavigateTo(ViewIDs.TestSub);
    }
    
    [RelayCommand]
    private void Exit()
    {
        Navigator.Quit();
    }

    protected override Task LoadAsync()
    {
        return Task.CompletedTask;
    }
}