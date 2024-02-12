using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Utils;
using Myra.Graphics2D;
using Myra.Platform;

namespace MintyCore.UI;

[Singleton<IUiRenderer>(SingletonContextFlags.NoHeadless)]
internal class UiRenderer : IUiRenderer
{
    public UiRenderer(ITextureManager textureManager)
    {
        TextureManager = textureManager;
        
        _renderData = new UiRenderData();
        _renderData.BeginRecording();
    }

    /// <inheritdoc />
    public ITexture2DManager TextureManager { get; }
    
    public required IInputDataManager InputDataManager { private get; [UsedImplicitly] init; } 

    /// <inheritdoc />
    public RendererType RendererType => RendererType.Quad;

    /// <inheritdoc />
    public Rectangle Scissor { get; set; }

    private readonly UiRenderData _renderData;

    /// <inheritdoc />
    public void Begin(TextureFiltering textureFiltering)
    {
        //Do nothing
    }

    /// <inheritdoc />
    public void End()
    {
        //Do nothing
    }

    /// <inheritdoc />
    public void DrawSprite(object texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth)
    {
        throw new NotSupportedException();
    }
    
    /// <inheritdoc />
    public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
        ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
    {
        if(texture is not FontTextureWrapper fontTexture)
            throw new InvalidOperationException("Only FontTextureWrapper is supported");

        var singleData = new RectangleRenderData(ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
        _renderData.AddDraw(Scissor, fontTexture, ref singleData);
    }

    /// <inheritdoc />
    public void ApplyRenderData()
    {
        _renderData.EndRecording();
        InputDataManager.SetSingletonInputData(RenderInputDataIDs.Ui, _renderData.ToInputData());
        _renderData.BeginRecording();
    }
}