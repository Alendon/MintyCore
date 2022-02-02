using System;
using System.Numerics;
using MintyCore.Render;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public class MainMenu : ElementContainer
{
    private ButtonElement _buttonElement;
    private TextBox _textBox;
    
    public MainMenu( Action startGame ) : base(new() { Extent = VulkanEngine.SwapchainExtent, Offset = new(0,0) })
    {
        IsRootElement = true;
        Engine.Window.GetWindow().FramebufferResize += OnResize;


        _buttonElement =
            new ButtonElement(new Rect2D(new() { X = 350, Y = 200 }, new Extent2D() { Height = 50, Width = 50 }));
        AddElement(_buttonElement);
        _buttonElement.OnLeftClickCb += startGame;

        _textBox = new TextBox(new Vector2(Layout.Extent.Width / 2f, Layout.Extent.Height - 30), "Main Menu");
        AddElement(_textBox);

        var playText = new TextBox(new Offset2D(420, 210), "Play");
        AddElement(playText);
    }

    private void OnResize(Vector2D<int> obj)
    {
        Layout = new() { Extent = new Extent2D((uint?)obj.X, (uint?)obj.Y), Offset = Layout.Offset };
        Resize(Layout.Extent);
    }

    public override void Dispose()
    {
        base.Dispose();
        Engine.Window.GetWindow().FramebufferResize -= OnResize;
    }
}