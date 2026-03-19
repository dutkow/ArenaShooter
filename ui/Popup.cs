using Godot;
using System;

public partial class Popup : Control
{
    [Export] Label _titleLabel;
    [Export] Label _descriptionLabel;

    [Export] Button _confirmButton;

    public Action ConfirmButtonPressed;

    public void Push(string titleText, string descriptionText, string confirmButtonText, Action onConfirm)
    {
        _titleLabel.Text = titleText;
        _descriptionLabel.Text = descriptionText;
        _confirmButton.Text = confirmButtonText;

        _confirmButton.Pressed += onConfirm;

        GD.Print($"push popup ran");
        
        Show();
    }

    public void OnConfirmButtonPressed()
    {
        ConfirmButtonPressed?.Invoke();
        ConfirmButtonPressed = null;
        Hide();
    }
}
