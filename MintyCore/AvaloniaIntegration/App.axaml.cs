using Avalonia;
using Avalonia.Markup.Xaml;

namespace MintyCore.AvaloniaIntegration;

public class App : Application {

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

}
