using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MintyCore.UI;

namespace TestMod;

public partial class TestControlModel : ViewModel
{
    
    [ObservableProperty]
    private string _message = "Hello, World!";
    
    protected override Task LoadAsync()
    {
        
        return Task.CompletedTask;
    }
}