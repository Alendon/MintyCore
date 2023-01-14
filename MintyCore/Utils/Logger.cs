using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MintyCore.Utils;

/// <summary>
/// 
/// </summary>
[PublicAPI]
public static class Logger
{
    private static TextWriter? _logWriter;
    
    internal static void InitializeLog()
    {
        var executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NullReferenceException();
        var dateTime = DateTime.Now.ToString("yyyy.mm.dd_hh.mm.ss", CultureInfo.InvariantCulture);
        
        var logFolderPath = Path.Combine(executionPath, "logs");
        
        var logFolder = new DirectoryInfo(logFolderPath);
        
        if (!logFolder.Exists)
        {
            logFolder.Create();
        }
        
        var logFile = new FileInfo(Path.Combine(logFolderPath, $"log_{dateTime}.txt"));

        var logStream = logFile.CreateText();
        _logWriter = TextWriter.Synchronized(logStream);
        
        WriteLog($"{logFile.Name} Logger initialised.", LogImportance.Info, "Logger");
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="logPrefix"></param>
    /// <param name="importance"></param>
    /// <returns></returns>
    public static bool AssertAndLog(bool condition, string message, string logPrefix, LogImportance importance)
    {
        if (condition) return true;

        WriteLog(message, importance, logPrefix);
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="logPrefix"></param>
    /// <param name="importance"></param>
    /// <returns></returns>
    public static bool AssertAndLog(bool condition,
        [InterpolatedStringHandlerArgument("condition")]
        AssertInterpolationHandler message, string logPrefix, LogImportance importance)
    {
        if (condition) return true;

        WriteLog(message.ToString(), importance, logPrefix);
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="logPrefix"></param>
    public static void AssertAndThrow([DoesNotReturnIf(false)] bool condition, string message, string logPrefix)
    {
        if (!condition) WriteLog(message, LogImportance.Exception, logPrefix);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="logPrefix"></param>
    public static void AssertAndThrow([DoesNotReturnIf(false)] bool condition,
        [InterpolatedStringHandlerArgument("condition")]
        AssertInterpolationHandler message,
        string logPrefix)
    {
        if (!condition) WriteLog(message.ToString(), LogImportance.Exception, logPrefix);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="log"></param>
    /// <param name="importance"></param>
    /// <param name="logPrefix"></param>
    /// <param name="printInConsole"></param>
    /// <exception cref="MintyCoreException"></exception>
    public static void WriteLog(string log, LogImportance importance, string logPrefix,
        bool printInConsole = true)
    {
        if(_logWriter is null)
            throw new MintyCoreException("Logger not initialised.");
        
        var localDate = DateTime.Now;

        var logLine = $"[{localDate:G}] [{importance}] [{logPrefix}] {log}";
        
        _logWriter.WriteLine(logLine);
        
        if (printInConsole)
            Console.WriteLine(logLine);
        
        if (importance != LogImportance.Exception) return;
        
        _logWriter.Flush();
        throw new MintyCoreException(log);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void AppendLogToFile()
    {
        _logWriter?.Flush();
    }
    
    internal static void CloseLog()
    {
        _logWriter?.Dispose();
        _logWriter = null;
    }
}

/// <summary>
/// Struct which wraps the DefaultInterpolatedStringHandler, but only interpolate the string if the assertion is not true.
/// </summary>
[InterpolatedStringHandler]
public ref struct AssertInterpolationHandler
{
    private DefaultInterpolatedStringHandler _internalHandler;
    private readonly bool _active;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="literalLength"></param>
    /// <param name="formattedCount"></param>
    /// <param name="condition"></param>
    public AssertInterpolationHandler(int literalLength, int formattedCount, bool condition)
    {
        _active = !condition;

        _internalHandler = _active ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
    }


    /// <inheritdoc />
    public override string ToString()
    {
        return _active ? _internalHandler.ToString() : string.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string ToStringAndClear()
    {
        return _active ? _internalHandler.ToString() : string.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void AppendLiteral(string value)
    {
        if (_active) _internalHandler.AppendLiteral(value);
    }

    #region AppendFormatted overloads
    // ReSharper disable MethodOverloadWithOptionalParameter

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void AppendFormatted<T>(T value)
    {
        if (_active) _internalHandler.AppendFormatted(value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format"></param>
    /// <typeparam name="T"></typeparam>
    public void AppendFormatted<T>(T value, string? format)
    {
        if (_active) _internalHandler.AppendFormatted(value, format);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="alignment"></param>
    /// <typeparam name="T"></typeparam>
    public void AppendFormatted<T>(T value, int alignment)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="alignment"></param>
    /// <param name="format"></param>
    /// <typeparam name="T"></typeparam>
    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (_active) _internalHandler.AppendFormatted(value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="alignment"></param>
    /// <param name="format"></param>
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void AppendFormatted(string? value)
    {
        if (_active) _internalHandler.AppendFormatted(value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="alignment"></param>
    /// <param name="format"></param>
    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
        
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="alignment"></param>
    /// <param name="format"></param>
    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }
    // ReSharper restore MethodOverloadWithOptionalParameter
    #endregion
}

/// <summary>
/// 
/// </summary>
public enum LogImportance
{
    /// <summary>
    /// 
    /// </summary>
    Debug,
    /// <summary>
    /// 
    /// </summary>
    Info,
    /// <summary>
    /// 
    /// </summary>
    Warning,
    /// <summary>
    /// 
    /// </summary>
    Error,
    /// <summary>
    /// 
    /// </summary>
    Exception
}