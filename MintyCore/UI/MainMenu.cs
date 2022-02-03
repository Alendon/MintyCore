using System;
using System.Numerics;
using MintyCore.Identifications;
using MintyCore.Render;
using Silk.NET.Maths;

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
    public MainMenu( Action startGame ) : base(new Layout(new Vector2(0,0), new Vector2(1,1)))
    {
        IsRootElement = true;
        Engine.Window!.WindowInstance.FramebufferResize += OnResize;


        var buttonElement = new ButtonElement(new Layout(new Vector2(0.3f, 0.5f), new Vector2(0.1f,0.1f)));
        AddElement(buttonElement);
        buttonElement.OnLeftClickCb += startGame;

        var textBox = new TextBox(new Layout(new Vector2(0.2f,0), new Vector2(0.6f, 0.2f)), "Main Menu", FontIDs.Akashi);
        AddElement(textBox);

        var playText = new TextBox(new Layout(new Vector2(0.4f, 0.5f), new Vector2(0.1f, 0.3f)), "Play", FontIDs.Akashi);
        AddElement(playText);

        var textField = new TextField(new Layout(new Vector2(0, 0.8f), new Vector2(1, 0.2f)), FontIDs.Akashi);
        AddElement(textField);
    }

    private void OnResize(Vector2D<int> obj)
    {
        _pixelSize = new Vector2(obj.X, obj.Y);
        Resize();
    }

    private Vector2 _pixelSize = new(VulkanEngine.SwapchainExtent.Width, VulkanEngine.SwapchainExtent.Height);

    /// <inheritdoc />
    public override Vector2 PixelSize => _pixelSize;

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        Engine.Window!.WindowInstance.FramebufferResize -= OnResize;
    }
}