using Avalonia.Controls;
using MintyCore.Registries;
using MintyCore.UI;
using TestMod.Identifications;

namespace TestMod;

public partial class TestMainMenuView : UserControl
{
    public TestMainMenuView()
    {
        InitializeComponent();
    }
    
    [RegisterView("test_main_menu")]
    public static ViewDescription<TestMainMenuView> Desc => new(ViewModelIDs.TestMainMenu);
}