using System;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Maths;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MintyCore.UI;

/// <summary>
///     Ui element representing the main menu
/// </summary>
public class MainMenu : ElementContainer
{
    private readonly Image _background;
    private readonly TextField _playerId;
    private readonly TextField _playerName;

    private readonly TextField _targetAddress;
    private readonly TextField _targetPort;

    private string _lastId = string.Empty;
    private string _lastPort = string.Empty;

    private SizeF _pixelSize = new(VulkanEngine.SwapchainExtent.Width, VulkanEngine.SwapchainExtent.Height);

    private ulong _playerIdValue;
    private ushort _targetPortValue;

    /// <summary>
    ///     Constructor
    /// </summary>
    public MainMenu() : base(new RectangleF(new PointF(0, 0), new SizeF(1, 1)))
    {
        IsRootElement = true;
        Engine.Window!.WindowInstance.FramebufferResize += OnResize;

        var title = new TextBox(new RectangleF(0.3f, 0, 0.4f, 0.2f), "Main Menu", FontIDs.Akashi, useBorder: false);
        title.IsActive = true;
        AddElement(title);

        const ushort fontSize = 35;

        _targetAddress = new TextField(new RectangleF(0.1f, 0.4f, 0.25f, 0.1f), FontIDs.Akashi,
            horizontalAlignment: HorizontalAlignment.Left, hint: "Target address", desiredFontSize: fontSize);
        _targetAddress.IsActive = true;
        AddElement(_targetAddress);

        _targetPort = new TextField(new RectangleF(0.1f, 0.5f, 0.25f, 0.1f), FontIDs.Akashi,
            horizontalAlignment: HorizontalAlignment.Left, hint: "Target port", desiredFontSize: fontSize);
        _targetPort.IsActive = true;
        AddElement(_targetPort);

        _playerName = new TextField(new RectangleF(0.65f, 0.4f, 0.25f, 0.1f), FontIDs.Akashi,
            horizontalAlignment: HorizontalAlignment.Left, hint: "Player name", desiredFontSize: fontSize);
        _playerName.IsActive = true;
        AddElement(_playerName);

        _playerId = new TextField(new RectangleF(0.65f, 0.5f, 0.25f, 0.1f), FontIDs.Akashi,
            horizontalAlignment: HorizontalAlignment.Left, hint: "Player id", desiredFontSize: fontSize);
        _playerId.IsActive = true;
        AddElement(_playerId);

        var play = new Button(new RectangleF(0.4f, 0.4f, 0.2f, 0.1f), "Play Local", fontSize);
        play.IsActive = true;
        play.OnLeftClickCb += OnPlayLocal;
        AddElement(play);

        var connectToServer = new Button(new RectangleF(0.4f, 0.5f, 0.2f, 0.1f), "Connect to Server", fontSize);
        connectToServer.IsActive = true;
        connectToServer.OnLeftClickCb += OnConnectToServer;
        AddElement(connectToServer);

        var createServer = new Button(new RectangleF(0.4f, 0.8f, 0.2f, 0.1f), "Create Server", fontSize);
        createServer.IsActive = true;
        createServer.OnLeftClickCb += OnCreateServer;
        AddElement(createServer);

        _background = ImageHandler.GetImage(ImageIDs.MainMenuBackground);
    }

    /// <inheritdoc />
    public override Image<Rgba32>? Image
    {
        get
        {
            CombinedImage?.Mutate(context =>
            {
                ImageBrush brush = new(_background);
                DrawingOptions options = new();
                context.Fill(options, brush, new RectangleF(0, 0, CombinedImage.Width, CombinedImage.Height));
            });
            return base.Image;
        }
    }

    /// <inheritdoc />
    public override SizeF PixelSize => _pixelSize;

    private void OnPlayLocal()
    {
        Engine.MainMenu = null;
        MainUiRenderer.SetMainUiContext(null);

        Engine.SetGameType(GameType.Local);

        PlayerHandler.LocalPlayerId = _playerIdValue != 0 ? _playerIdValue : 1;
        PlayerHandler.LocalPlayerName = _playerName.InputText.Length != 0 ? _playerName.InputText : "Local";

        Engine.LoadMods(ModManager.GetAvailableMods(true));

        WorldHandler.CreateWorlds(GameType.Server);

        Engine.CreateServer(_targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);
        Engine.ConnectToServer(_targetAddress.InputText.Length == 0 ? "localhost" : _targetAddress.InputText,
            _targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);

        Engine.GameLoop();
    }

    private void OnConnectToServer()
    {
        Engine.MainMenu = null;
        MainUiRenderer.SetMainUiContext(null);

        if (_playerIdValue == 0)
        {
            Logger.WriteLog("Player id cannot be 0", LogImportance.Error, "MintyCore");
            return;
        }

        if (_playerName.InputText.Length == 0)
        {
            Logger.WriteLog("Player name cannot be empty", LogImportance.Error, "MintyCore");
            return;
        }

        if (_targetAddress.InputText.Length == 0)
        {
            Logger.WriteLog("Target server cannot be empty", LogImportance.Error, "MintyCore");
            return;
        }

        Engine.SetGameType(GameType.Client);


        PlayerHandler.LocalPlayerId = _playerIdValue;
        PlayerHandler.LocalPlayerName = _playerName.InputText;

        Engine.ConnectToServer(_targetAddress.InputText,
            _targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);

        Engine.GameLoop();
    }

    private void OnCreateServer()
    {
        Engine.MainMenu = null;
        MainUiRenderer.SetMainUiContext(null);

        Engine.SetGameType(GameType.Server);

        Engine.LoadMods(ModManager.GetAvailableMods(true));

        WorldHandler.CreateWorlds(GameType.Server);

        Engine.CreateServer(_targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);

        Engine.GameLoop();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (!ulong.TryParse(_playerId.InputText, out _playerIdValue) && _playerId.InputText.Length != 0)
            _playerId.InputText = _lastId;
        else
            _lastId = _playerId.InputText;

        if (!ushort.TryParse(_targetPort.InputText, out _targetPortValue) && _targetPort.InputText.Length != 0)
            _targetPort.InputText = _lastPort;
        else
            _lastPort = _targetPort.InputText;
    }

    private void OnResize(Vector2D<int> obj)
    {
        _pixelSize = new SizeF(obj.X, obj.Y);
        Resize();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
        Engine.Window!.WindowInstance.FramebufferResize -= OnResize;
    }
}