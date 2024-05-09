using System;
using Serilog;

namespace MintyCore;

public class Program
{
    /// <summary>
    ///     The entry/main method of the engine
    /// </summary>
    /// <remarks>Is public to allow easier mod development</remarks>
    public static void Main(string[] args)
    {
        var engine = new Engine(args);

        engine.Prepare();

        try
        {
            engine.Init();
            engine.RunGame();
            engine.CleanUp();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Exception occurred while running game");
            throw;
        }
        finally
        {
            Log.Information("Shutting down");
            Log.CloseAndFlush();
        }
    }
}