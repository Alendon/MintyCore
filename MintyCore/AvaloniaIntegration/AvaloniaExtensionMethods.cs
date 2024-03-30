using System;
using AvaloniaLogEventLevel = Avalonia.Logging.LogEventLevel;
using SerilogLogEventLevel = Serilog.Events.LogEventLevel;

namespace MintyCore.AvaloniaIntegration;

public static class AvaloniaExtensionMethods
{
    public static SerilogLogEventLevel ToSerilogLogEventLevel(this AvaloniaLogEventLevel level)
        => level switch
        {
            AvaloniaLogEventLevel.Debug => SerilogLogEventLevel.Debug,
            AvaloniaLogEventLevel.Error => SerilogLogEventLevel.Error,
            AvaloniaLogEventLevel.Fatal => SerilogLogEventLevel.Fatal,
            AvaloniaLogEventLevel.Information => SerilogLogEventLevel.Information,
            AvaloniaLogEventLevel.Warning => SerilogLogEventLevel.Warning,
            AvaloniaLogEventLevel.Verbose => SerilogLogEventLevel.Verbose,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
}