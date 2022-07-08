using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MintyCore.Utils;

//TODO Jannis cleaning is your work
[PublicAPI]
public class Logger
{
    private static string? _path1;
    private static string? _pathDbo;

    public static string PathRaw = "";
    public static string Output = "";
    public static string Stack = "";
    public static bool Initialised = false;
    public static DateTime LocalDate = DateTime.Now;
    public static string? TimeDate1;
    public static string? Time;
    public static string? Date;
    public static string[] TimeDate = new string[2];

    public static string PathLogFolder = "";

    public static string LogFileName = "";

    //E:\Projekte\source\repos\ConsoleApp41\ConsoleApp41\bin\Debug\netcoreapp3.1

    //Stores Temporary the Log Files with the optional subfolder
    private static readonly ConcurrentQueue<(string, string?)> _logWithSubFolderQueue = new();


    public static void InitializeLog()
    {
        PathRaw = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NullReferenceException();
        var localDate = DateTime.Now;
        TimeDate1 = localDate.ToString(DateTimeFormatInfo.InvariantInfo);
        TimeDate = TimeDate1.Split(' ');
        Time = TimeDate[1];
        Date = TimeDate[0];
        PathLogFolder = $"{PathRaw}/logs/";
        LogFileName = Date.Replace('/', '.') + "_" + Time.Replace(':', '.') + ".log";

        _path1 = $"{PathRaw}/logs/" + Date + "_" + Time.Replace(':', '.') + ".log";
        _pathDbo = $"{PathRaw}/logs/" + Date + "_" + Time.Replace(':', '.') + "-DebugOnly.log";


        if (!Directory.Exists($"{PathRaw}/logs/"))
            Directory.CreateDirectory($"{PathRaw}/logs/");
        WriteLog($"{_path1} Logger initialised.", LogImportance.Info, "Logger");
    }

    public static bool AssertAndLog(bool condition, string message, string logPrefix, LogImportance importance)
    {
        if (condition) return true;

        WriteLog(message, importance, logPrefix);
        return false;
    }

    public static bool AssertAndLog(bool condition,
        [InterpolatedStringHandlerArgument("condition")]
        AssertInterpolationHandler message, string logPrefix, LogImportance importance)
    {
        if (condition) return true;

        WriteLog(message.ToString(), importance, logPrefix);
        return false;
    }

    public static void AssertAndThrow([DoesNotReturnIf(false)] bool condition, string message, string logPrefix)
    {
        if (!condition) WriteLog(message, LogImportance.Exception, logPrefix);
    }

    public static void AssertAndThrow([DoesNotReturnIf(false)] bool condition,
        [InterpolatedStringHandlerArgument("condition")]
        AssertInterpolationHandler message,
        string logPrefix)
    {
        if (!condition) WriteLog(message.ToString(), LogImportance.Exception, logPrefix);
    }

    public static void WriteLog(string log, LogImportance importance, string logPrefix, string? subFolder = null,
        bool printInUnity = true)
    {
        //TODO

        //writes the Log, as the name says^^.
        var localDate = DateTime.Now;

        //Y Combines the given Logentry with the Date, the given Importance and in the Case of DBO(DebugOnly) the Funktion its called from.
        var logLine = $"[{localDate.ToString("G")}][{importance}][{logPrefix}]{log}";

        _logWithSubFolderQueue.Enqueue((logLine, subFolder));
        if (printInUnity)
            Console.WriteLine(logLine);
        if (importance == LogImportance.Exception) throw new MintyCoreException(log);
    }

    public static void AppendLogToFile()
    {
        while (_logWithSubFolderQueue.TryDequeue(out var res))
        {
            var (logLine, logFolder) = res;

            var logFilePath = $"{PathLogFolder}{(logFolder != null ? logFolder + "/" : string.Empty)}{LogFileName}";

            if (!Directory.Exists($"{PathLogFolder}{(logFolder != null ? logFolder + "/" : string.Empty)}"))
                Directory.CreateDirectory($"{PathLogFolder}{(logFolder != null ? logFolder + "/" : string.Empty)}");


            File.AppendAllText(logFilePath, logLine + Environment.NewLine);
        }
    }
}

/// <summary>
/// Struct which wraps the DefaultInterpolatedStringHandler, but only interpolate the string if the assertion is not true.
/// </summary>
[InterpolatedStringHandler]
public ref struct AssertInterpolationHandler
{
    private DefaultInterpolatedStringHandler _internalHandler;
    private bool _active;

    public AssertInterpolationHandler(int literalLength, int formattedCount, bool condition)
    {
        _active = !condition;

        _internalHandler = _active ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
    }

    public override string ToString()
    {
        return _active ? _internalHandler.ToString() : string.Empty;
    }

    public string ToStringAndClear()
    {
        return _active ? _internalHandler.ToString() : string.Empty;
    }

    public void AppendLiteral(string value)
    {
        if (_active) _internalHandler.AppendLiteral(value);
    }

    #region AppendFormatted overloads

    public void AppendFormatted<T>(T value)
    {
        if (_active) _internalHandler.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T value, string? format)
    {
        if (_active) _internalHandler.AppendFormatted(value, format);
    }

    public void AppendFormatted<T>(T value, int alignment)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (_active) _internalHandler.AppendFormatted(value);
    }

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(string? value)
    {
        if (_active) _internalHandler.AppendFormatted(value);
    }

    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
    {
        if (_active) _internalHandler.AppendFormatted(value, alignment, format);
    }

    #endregion
}

public enum LogImportance
{
    Debug,
    Info,
    Warning,
    Error,
    Exception
}