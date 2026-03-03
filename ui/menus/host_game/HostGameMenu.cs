using Godot;
using System;

public partial class HostGameMenu : Control
{
    [Export] PackedScene _testLevel;

    [Export] Button _hostGameButton;

    [Export] OptionButton _mapOptionButton;

    [Export] Button _backButton;

    public override void _Ready()
    {
        base._Ready();

        _hostGameButton.Pressed += OnHostGameButtonPressed;
        _backButton.Pressed += OnBackButtonPressed;


        for (int i = 0; i < GameData.Instance.MultiplayerMaps.Maps.Count; ++i)
        {
            var mapInfo = GameData.Instance.MultiplayerMaps.Maps[i];
            _mapOptionButton.AddItem(mapInfo.Name);
            _mapOptionButton.SetItemMetadata(i, mapInfo.ID);
        }
    }

    public void Open()
    {
        Show();
    }

    public void OnHostGameButtonPressed()
    {
        var serverInfo = new ServerInfo();
        serverInfo.Name = "Test Server";
        serverInfo.MapID = (string)_mapOptionButton.GetItemMetadata(_mapOptionButton.Selected);
        NetworkSession.Instance.HostLanServer(serverInfo);
    }

    public void OnBackButtonPressed()
    {
        Hide();
    }
}
