using Godot;
using System;

public class PossessableComponent
{
    public Controller Controller { get; private set; }
    private bool _inputEnabled = false;

    public bool IsPossessed => Controller != null;
    public bool InputActive => IsPossessed && _inputEnabled;

    public void OnPossessed(Controller controller)
    {
        Controller = controller;
        _inputEnabled = true;

        // Optional: do role/network setup here
    }

    public void OnUnpossessed()
    {
        Controller = null;
        _inputEnabled = false;
    }

    public void SetInputEnabled(bool value)
    {
        _inputEnabled = value;
    }
}