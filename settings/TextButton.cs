using Godot;
using System;

[Tool]
[GlobalClass]
public partial class TextButton : ButtonWrapper
{
    private string _buttonText;

    [Export]
    public string ButtonText
    {
        get => _buttonText;
        set
        {
            _buttonText = value;
            if(Button != null)
            {
                Button.Text = value;
            }
        }
    }
}
