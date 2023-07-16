
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace MintyCore.Modding;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public interface IMod
{
    void Dispose();
    ushort ModId { get; set; }
    void PreLoad();
    void Load();
    void PostLoad();
    void Unload();
}