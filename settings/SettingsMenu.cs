using Godot;
using System;

public partial class SettingsMenu : MenuPanel
{
    [Export] TextButton DiscardButton;
    [Export] TextButton RestoreDefaultsButton;
    [Export] TextButton CloseButton;

    public override void _Ready()
    {
        base._Ready();

        DiscardButton.Pressed += DiscardChanges;
        CloseButton.Pressed += SaveChangesAndClose;
        RestoreDefaultsButton.Pressed += OnRestoreDefaultsButtonPressed;

        InputManager.Instance.NavigateBackPressed += Close;
        SettingsManager.Instance.SettingsDirtyChanged += OnSettingsDirtyChanged;
    }

    public void DiscardChanges()
    {

        InputManager.Instance.DiscardPendingInputChanges();



        DiscardButton.Disabled = true;


        SettingsManager.Instance.RevertAllPendingChanges();
        //Close();
    }

    public void SaveChangesAndClose()
    {
        SettingsManager.Instance.AcceptAllPendingChanges();
        Close();
    }

    public void OnSettingsDirtyChanged(bool isDirty)
    {
        DiscardButton.Disabled = !isDirty;

        CloseButton.ButtonText = isDirty ? "SAVE" : "CLOSE";
    }

    public void OnRestoreDefaultsButtonPressed()
    {
        InputManager.Instance.RestoreAllInputsToDefaults();
    }

    public override void Close()
    {
        base.Close();

        if(InputManager.Instance.InputBindingsDirty)
        {
            InputManager.Instance.AcceptPendingInputChanges();
        }
    }
}
