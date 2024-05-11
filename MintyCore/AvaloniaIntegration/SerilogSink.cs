using System.Diagnostics.CodeAnalysis;
using Avalonia.Logging;


namespace MintyCore.AvaloniaIntegration;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "This is just a forwarder to Serilog.")]
internal class SerilogSink : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area)
    {
        return Serilog.Log.IsEnabled(level.ToSerilogLogEventLevel()) && area != LogArea.Layout;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Serilog.Log.Write(level.ToSerilogLogEventLevel(), messageTemplate);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
        params object?[] propertyValues)
    {
        Serilog.Log.Write(level.ToSerilogLogEventLevel(), messageTemplate, propertyValues);
    }
}