using MintyCore.Components.Client;
using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> class which contains all <see cref="ECS.IComponent" /> ids
/// </summary>
public static class ComponentIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Common.Position" />
    /// </summary>
    public static Identification Position { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Common.Rotation" />
    /// </summary>
    public static Identification Rotation { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Common.Scale" />
    /// </summary>
    public static Identification Scale { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Common.Transform" />
    /// </summary>
    public static Identification Transform { get; internal set; }


    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="RenderAble" />
    /// </summary>
    public static Identification Renderable { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Client.Camera" />
    /// </summary>
    public static Identification Camera { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Common.Physic.Mass" />
    /// </summary>
    public static Identification Mass { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Components.Common.Physic.Collider" />
    /// </summary>
    public static Identification Collider { get; internal set; }

    /// <summary>
    /// <see cref="Identification"/> of the <see cref="MintyCore.Components.Client.InstancedRenderAble"/>
    /// </summary>
    public static Identification InstancedRenderAble { get; set; }
}