using Godot;
using System;

public partial class PlayerHud : Control
{
    [Export] private HealthStatPanel _armorPanel;
    [Export] private HealthStatPanel _healthPanel;

    public override void _Ready()
    {
        base._Ready();


        //var playerState = ClientGame.Instance.LocalPlayerState;

        //_armorPanel.Initialize(playerState);
        //_healthPanel.Initialize(playerState);

    }

}
