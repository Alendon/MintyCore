using Avalonia.Controls.Embedding;

namespace MintyCore.AvaloniaIntegration;

public class MintyCoreTopLevel : EmbeddableControlRoot
{
    public MintyCoreTopLevelImpl Impl { get; }
    
    public MintyCoreTopLevel(MintyCoreTopLevelImpl impl)
        : base(impl)
        => Impl = impl;
}