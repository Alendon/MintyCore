using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MintyCore.Registries;
using MintyCore.UI;
using TestMod.Identifications;

namespace TestMod;

public partial class TestMainView : UserControl
{
    public TestMainView()
    {
        InitializeComponent();
    }
    
    [RegisterView("test_main")]
    public static ViewDescription<TestMainView> Desc => new(ViewModelIDs.TestMain);
}