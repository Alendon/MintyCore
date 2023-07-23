namespace MintyCore.Generator.Tests;

public static class SharedFakeCode
{

    public const string RegistryBaseCode = """
using System;

namespace MintyCore.Modding.Attributes
{                                 
    public class RegistryAttribute : Attribute
    {
        public RegistryAttribute(string id, string? resourceFolder = null)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RegisterMethodAttribute : Attribute
    {
        public RegisterMethodAttribute(ObjectRegistryPhase phase, 
            RegisterMethodOptions options = RegisterMethodOptions.None)
        {
        }
    }
    
    [Flags]
    public enum RegisterMethodOptions
    {
        None = 0,
        HasFile = 1 << 0,
        UseExistingId = 1 << 1
    }
}

namespace MintyCore.Modding
{
    public interface IRegistry
    {                         
                              
    }            

    public enum ObjectRegistryPhase
    {
        None = 0,
        Pre,
        Main,
        Post
    }             
}
""";

    public const string TestMod = """
using MintyCore.Modding;

namespace TestMod;

public class Test : IMod
{
    public void Dispose() { }
    public ushort ModId { get; set; }
    public void PreLoad() { }
    public void Load() { }
    public void PostLoad() { }
    public void Unload(){ }
}
""";
    
    public const string Identification = """
namespace MintyCore.Utils;

public struct Identification{}
""";
    
}