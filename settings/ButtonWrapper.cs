using Godot;
using System;
using System.Security.Cryptography.X509Certificates;

[Tool]
[GlobalClass]
public partial class ButtonWrapper : Control
{
    public event Action Pressed;

    [Export] public Button Button;


    private bool _disabled;

    [Export] 
    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            if(Button != null)
            {
                Button.Disabled = value;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Button.Pressed += () => Pressed?.Invoke();

        Button.Disabled = Disabled;
    }
}
