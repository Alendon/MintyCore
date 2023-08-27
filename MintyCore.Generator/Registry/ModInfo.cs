namespace MintyCore.Generator.Registry;

public record struct ModInfo
{
    public ModInfo()
    {
    }
    
    
    public string Namespace { get; set; } = "";
    public string ClassName { get; set; } = "";
}