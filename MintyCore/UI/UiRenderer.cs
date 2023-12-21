using System;
using System.Drawing;
using System.Numerics;
using System.Threading;
using FontStashSharp;
using FontStashSharp.Interfaces;
using MintyCore.Render.Managers.Interfaces;
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
        
        _workRenderData = new UiRenderData();
        _presentRenderData = new UiRenderData();
        
        _workRenderData.BeginRecording();
        
        _noWriteWait = () => _renderDataLock.WaitingWriteCount == 0;
        _releaseLock = () => _renderDataLock.ExitReadLock();
    }

    /// <inheritdoc />
    public ITexture2DManager TextureManager { get; }

    /// <inheritdoc />
    public RendererType RendererType => RendererType.Quad;

    /// <inheritdoc />
    public Rectangle Scissor { get; set; }

    private UiRenderData _workRenderData;
    private UiRenderData _presentRenderData;
    private readonly ReaderWriterLockSlim _renderDataLock = new(LockRecursionPolicy.NoRecursion);
    private readonly Func<bool> _noWriteWait;
    private readonly Action _releaseLock;

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

        var singleData = new UiRenderData.RectangleRenderData(ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
        _workRenderData.AddDraw(Scissor, fontTexture, ref singleData);
    }

    /// <inheritdoc />
    public void SwapRenderData()
    {
        _renderDataLock.EnterWriteLock();
        try
        {
            _workRenderData.EndRecording();
            (_workRenderData, _presentRenderData) = (_presentRenderData, _workRenderData);
            _workRenderData.BeginRecording();
        }
        finally
        {
            _renderDataLock.ExitWriteLock();
        }
    }
    
    /// <inheritdoc />
    public DisposeActionWrapper GetCurrentRenderData(out UiRenderData renderData)
    {
        //if multiple consumers exists, they could prevent the render data from being swapped
        //so we wait until no write lock requests are pending
        SpinWait.SpinUntil(_noWriteWait);
        
        _renderDataLock.EnterReadLock();
        renderData = _presentRenderData;
        return new DisposeActionWrapper(_releaseLock);
    }
    
    
}