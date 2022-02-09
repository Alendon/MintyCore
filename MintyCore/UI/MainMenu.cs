﻿using System;
using System.Numerics;
using MintyCore.Identifications;
using MintyCore.Render;
using Silk.NET.Maths;
using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace MintyCore.UI;

/// <summary>
/// Ui element representing the main menu
/// </summary>
public class MainMenu : ElementContainer
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="startGame"></param>
    public MainMenu(Action startGame) : base(new RectangleF(new PointF(0, 0), new SizeF(1, 1)))
    {
        IsRootElement = true;
        Engine.Window!.WindowInstance.FramebufferResize += OnResize;


        var buttonElement = new Button(new RectangleF(new PointF(0.3f, 0.5f), new SizeF(0.1f, 0.1f)), "Play");
        buttonElement.IsActive = true;
        AddElement(buttonElement);
        buttonElement.OnLeftClickCb += startGame;

        var textBox = new TextBox(new RectangleF(new PointF(0.2f, 0), new SizeF(0.6f, 0.2f)), "Main Menu",
            FontIDs.Akashi, useBorder: false);
        textBox.IsActive = true;
        AddElement(textBox);

        var textField = new TextField(new RectangleF(new PointF(0, 0.8f), new SizeF(1, 0.2f)), FontIDs.Akashi,
            horizontalAlignment: HorizontalAlignment.Left);
        textField.IsActive = true;
        AddElement(textField);
    }

    private void OnResize(Vector2D<int> obj)
    {
        _pixelSize = new SizeF(obj.X, obj.Y);
        Resize();
    }

    private SizeF _pixelSize = new (VulkanEngine.SwapchainExtent.Width, VulkanEngine.SwapchainExtent.Height);

    /// <inheritdoc />
    public override SizeF PixelSize => _pixelSize;

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        Engine.Window!.WindowInstance.FramebufferResize -= OnResize;
    }
}