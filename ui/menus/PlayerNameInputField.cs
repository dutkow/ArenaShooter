using Godot;
using System;

public partial class PlayerNameInputField : LineEdit
{
    public override void _Ready()
    {
        base._Ready();

        MaxLength = NetworkConstants.MAX_PLAYER_NAME_LENGTH;
    }
}
