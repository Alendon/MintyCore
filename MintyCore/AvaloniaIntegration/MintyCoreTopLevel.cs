using Avalonia.Controls.Embedding;

namespace MintyCore.AvaloniaIntegration;

/// <inheritdoc />
public class MintyCoreTopLevel : EmbeddableControlRoot
{
    /// <summary>
    /// Gets the implementation.
    /// </summary>
    public MintyCoreTopLevelImpl Impl { get; }

    /// <inheritdoc />
    public MintyCoreTopLevel(MintyCoreTopLevelImpl impl)
        : base(impl)
        => Impl = impl;
}