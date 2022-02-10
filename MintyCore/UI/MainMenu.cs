using System;
using System.Numerics;
using ENet;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network;
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
/// Ui element representing the main menu
/// </summary>
public class MainMenu : ElementContainer
{
    private TextField _playerName;
    private TextField _playerId;

    private TextField _targetAddress;
    private TextField _targetPort;

    private TextBox _title;
    private Button _createServer;
    private Button _connectToServer;
    private Button _play;

    private Image _background;

    private ulong _playerIdValue;
    private ushort _targetPortValue;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="startGame"></param>
    public MainMenu() : base(new RectangleF(new PointF(0, 0), new SizeF(1, 1)))
    {
        IsRootElement = true;
        Engine.Window!.WindowInstance.FramebufferResize += OnResize;

        _title = new TextBox(new(0.3f, 0, 0.4f, 0.2f), "Main Menu", FontIDs.Akashi, useBorder: false);
        _title.IsActive = true;
        AddElement(_title);

        ushort fontSize = 35;

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

        _play = new Button(new(0.4f, 0.4f, 0.2f, 0.1f), "Play Local", fontSize);
        _play.IsActive = true;
        _play.OnLeftClickCb += OnPlayLocal;
        AddElement(_play);

        _connectToServer = new Button(new(0.4f, 0.5f, 0.2f, 0.1f), "Connect to Server", fontSize);
        _connectToServer.IsActive = true;
        _connectToServer.OnLeftClickCb += OnConnectToServer;
        AddElement(_connectToServer);

        _createServer = new Button(new(0.4f, 0.8f, 0.2f, 0.1f), "Create Server", fontSize);
        _createServer.IsActive = true;
        _createServer.OnLeftClickCb += OnCreateServer;
        AddElement(_createServer);

        _background = ImageHandler.GetImage(ImageIDs.MainMenuBackground);
    }

    private void OnPlayLocal()
    {
        Engine.SetGameType(GameType.LOCAL);

        PlayerHandler.LocalPlayerId = _playerIdValue != 0 ? _playerIdValue : 1;
        PlayerHandler.LocalPlayerName = _playerName.InputText.Length != 0 ? _playerName.InputText : "Local";

        Engine.LoadMods(ModManager.GetAvailableMods());

        Engine.LoadServerWorld();

        Engine.CreateServer(_targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);
        Engine.ConnectToServer(_targetAddress.InputText.Length == 0 ? "localhost" : _targetAddress.InputText,
            _targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);

        Engine.GameLoop();
    }

    private void OnConnectToServer()
    {

        if (_playerIdValue == 0)
        {
            Logger.WriteLog("Player id cannot be 0", LogImportance.ERROR, "MintyCore");
            return;
        }

        if (_playerName.InputText.Length == 0)
        {
            Logger.WriteLog("Player name cannot be empty", LogImportance.ERROR, "MintyCore");
            return;
        }
        
        if (_targetAddress.InputText.Length == 0)
        {
            Logger.WriteLog("Target server cannot be empty", LogImportance.ERROR, "MintyCore");
            return;
        }
        
        Engine.SetGameType(GameType.CLIENT);


        PlayerHandler.LocalPlayerId = _playerIdValue;
        PlayerHandler.LocalPlayerName = _playerName.InputText;
        
        Engine.ConnectToServer(_targetAddress.InputText,
            _targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);

        Engine.GameLoop();
    }

    private void OnCreateServer()
    {
        Engine.SetGameType(GameType.SERVER);

        Engine.LoadMods(ModManager.GetAvailableMods());

        Engine.LoadServerWorld();

        Engine.CreateServer(_targetPortValue != 0 ? _targetPortValue : Constants.DefaultPort);

        Engine.GameLoop();
    }

    private string _lastId = String.Empty;
    private string _lastPort = String.Empty;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (!ulong.TryParse(_playerId.InputText, out _playerIdValue) && _playerId.InputText.Length != 0)
        {
            _playerId.InputText = _lastId;
        }
        else
        {
            _lastId = _playerId.InputText;
        }

        if (!ushort.TryParse(_targetPort.InputText, out _targetPortValue) && _targetPort.InputText.Length != 0)
        {
            _targetPort.InputText = _lastPort;
        }
        else
        {
            _lastPort = _targetPort.InputText;
        }
    }

    /// <inheritdoc />
    public override Image<Rgba32> Image
    {
        get
        {
            _image.Mutate(context =>
            {
                ImageBrush brush = new(_background);
                DrawingOptions options = new();
                context.Fill(options, brush, new RectangleF(0, 0, _image.Width, _image.Height));
            });
            return base.Image;
        }
    }

    private void OnResize(Vector2D<int> obj)
    {
        _pixelSize = new SizeF(obj.X, obj.Y);
        Resize();
    }

    private SizeF _pixelSize = new(VulkanEngine.SwapchainExtent.Width, VulkanEngine.SwapchainExtent.Height);

    /// <inheritdoc />
    public override SizeF PixelSize => _pixelSize;

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        Engine.Window!.WindowInstance.FramebufferResize -= OnResize;
    }
}