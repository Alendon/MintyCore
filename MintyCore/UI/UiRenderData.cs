using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FontStashSharp.Interfaces;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Registries;

namespace MintyCore.UI;

/// <summary>
///  Represents the render data for the UI
/// </summary>
[PublicAPI]
public class UiRenderData
{
    private List<(int start, int length)> _batchRanges = new();
    private List<Rectangle> _batchScissors = new();
    private List<FontTextureWrapper> _batchTextures = new();
    private List<RectangleRenderData> _batchData = new();

    private (int start, int length) _currentRange;
    private Rectangle _currentScissor;
    private FontTextureWrapper? _currentTexture;

    private bool _isRecording;

    /// <summary>
    ///  Begins recording the render data
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void BeginRecording()
    {
        if (_isRecording)
            throw new InvalidOperationException("Already recording");

        _batchRanges.Clear();
        _batchScissors.Clear();
        _batchTextures.Clear();
        _batchData.Clear();

        _currentRange = (0, 0);
        _currentScissor = default;
        _currentTexture = null;

        _isRecording = true;
    }

    /// <summary>
    ///  Ends recording the render data
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void EndRecording()
    {
        if (!_isRecording)
            throw new InvalidOperationException("Not recording");

        Flush();
        _isRecording = false;
    }

    /// <summary>
    ///  Adds a draw call to the render data
    /// </summary>
    /// <param name="scissor"> The scissor rectangle </param>
    /// <param name="texture"> The texture to use </param>
    /// <param name="data"> The render data </param>
    public void AddDraw(Rectangle scissor, FontTextureWrapper texture, ref RectangleRenderData data)
    {
        if (scissor != _currentScissor || texture != _currentTexture)
        {
            Flush();
            _currentScissor = scissor;
            _currentTexture = texture;
        }

        _batchData.Add(data);
        _currentRange.length++;
    }

    private void Flush()
    {
        if (_currentRange.length == 0)
            return;

        _batchRanges.Add(_currentRange);
        _batchScissors.Add(_currentScissor);
        _batchTextures.Add(_currentTexture ?? throw new InvalidOperationException());

        _currentRange = (_batchData.Count, 0);
    }

    /// <summary>
    ///  Creates a <see cref="UiRenderInputData"/> from the recorded data
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public UiRenderInputData ToInputData()
    {
        if (_isRecording)
            throw new InvalidOperationException("Still recording");

        return new UiRenderInputData([.._batchRanges], [.._batchScissors], [.._batchTextures], [.._batchData]);
    }
}

/// <summary>
///  Represents the input data for the UI renderer
/// </summary>
[PublicAPI]
public class UiRenderInputData(
    List<(int start, int length)> batchRanges,
    List<Rectangle> batchScissors,
    List<FontTextureWrapper> batchTextures,
    List<RectangleRenderData> batchData)
{
    private readonly List<(int start, int length)> _batchRanges = batchRanges;
    private readonly List<Rectangle> _batchScissors = batchScissors;
    private readonly List<FontTextureWrapper> _batchTextures = batchTextures;
    private readonly List<RectangleRenderData> _batchData = batchData;

    /// <summary>
    ///  Gets an enumerator for the render data
    /// </summary>
    /// <returns></returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    [RegisterSingletonInputData("ui")]
    internal static SingletonInputDataRegistryWrapper<UiRenderInputData> RegistryWrapper => new();

    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public struct Enumerator
    {
        private readonly UiRenderInputData _data;
        private int _index = -1;

        internal Enumerator(UiRenderInputData data)
        {
            _data = data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool MoveNext() => ++_index < _data._batchRanges.Count;

        /// <summary>
        /// 
        /// </summary>
        public CurrentBatch Current
        {
            get
            {
                var range = _data._batchRanges[_index];

                return new CurrentBatch
                {
                    Data = CollectionsMarshal.AsSpan(_data._batchData).Slice(range.start, range.length),
                    Scissor = _data._batchScissors[_index],
                    Texture = _data._batchTextures[_index]
                };
            }
        }
    }

    /// <summary/>
    [PublicAPI]
    public ref struct CurrentBatch
    {
        /// <summary/>
        public Span<RectangleRenderData> Data;

        /// <summary/>
        public Rectangle Scissor;

        /// <summary/>
        public FontTextureWrapper Texture;
    }
}
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference

/// <summary/>
[StructLayout(LayoutKind.Explicit, Size = sizeof(float) * 2 * 4 * 2 + sizeof(uint) * 4)]
[PublicAPI]
public unsafe struct RectangleRenderData
{
    [FieldOffset(0)] private Vector2 _vertexPositions;
    [FieldOffset(sizeof(float) * 2 * 4)] private Vector2 _uvCoords;

    [FieldOffset(sizeof(float) * 2 * 4 * 2)]
    private uint _colors;

    /// <summary/>
    public ref Vector2 Vertex(int index)
    {
        if (index is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(index));

        return ref Unsafe.Add(ref _vertexPositions, index);
    }

    /// <summary/>
    public ref Vector2 Uv(int index)
    {
        if (index is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(index));

        return ref Unsafe.Add(ref _uvCoords, index);
    }

    /// <summary/>
    public ref uint Color(int index)
    {
        if (index is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(index));

        return ref Unsafe.Add(ref _colors, index);
    }

    /// <summary/>
    public RectangleRenderData(ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
        ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
    {
        Vertex(0) = new Vector2(topLeft.Position.X, topLeft.Position.Y);
        Vertex(1) = new Vector2(topRight.Position.X, topRight.Position.Y);
        Vertex(2) = new Vector2(bottomLeft.Position.X, bottomLeft.Position.Y);
        Vertex(3) = new Vector2(bottomRight.Position.X, bottomRight.Position.Y);

        Uv(0) = topLeft.TextureCoordinate;
        Uv(1) = topRight.TextureCoordinate;
        Uv(2) = bottomLeft.TextureCoordinate;
        Uv(3) = bottomRight.TextureCoordinate;

        Color(0) = topLeft.Color.PackedValue;
        Color(1) = topRight.Color.PackedValue;
        Color(2) = bottomLeft.Color.PackedValue;
        Color(3) = bottomRight.Color.PackedValue;
    }
}
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
