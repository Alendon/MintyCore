// See https://aka.ms/new-console-template for more information


using Avalonia;
using MintyCore;

internal class Program
{
    public static void Main(string[] args)
    {
        throw new NotSupportedException("This project is not intended to be run directly.");
    }
    
    public static AppBuilder BuildAvaloniaApp()
    {
        return Engine.BuildAvaloniaApp();
    }
}