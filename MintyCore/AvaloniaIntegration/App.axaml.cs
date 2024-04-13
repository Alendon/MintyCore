using Avalonia;
using Avalonia.Markup.Xaml;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class App : Application {
    /// <inheritdoc />
    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

}
