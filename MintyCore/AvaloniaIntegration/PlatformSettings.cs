using Avalonia.Platform;

namespace MintyCore.AvaloniaIntegration;

internal class PlatformSettings : DefaultPlatformSettings
{
    public override PlatformColorValues GetColorValues()
    {
        return new PlatformColorValues
        {
            ThemeVariant = PlatformThemeVariant.Dark,
            ContrastPreference = ColorContrastPreference.NoPreference,
        };
    }
}