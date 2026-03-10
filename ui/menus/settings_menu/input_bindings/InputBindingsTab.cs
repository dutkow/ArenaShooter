using Godot;
using Godot.Collections;
using System;

public partial class InputBindingsTab : Control
{
    [Export] PackedScene _inputCategoryPanelScene;
    [Export] PackedScene _inputBindingSettingEntryScene;

    [Export] Container _inputBindingsContainer;


    public override void _Ready()
    {
        base._Ready();

        PopulateInputActionEntries();
    }

    public void ClearInputActionEntries()
    {
        foreach(Node child in _inputBindingsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    public void PopulateInputActionEntries()
    {
        ClearInputActionEntries();

        var keybindingProfile = InputManager.Instance.UserInputMappingProfile;
        foreach(var category in keybindingProfile.Categories)
        {
            var categoryHeader = (InputCategoryPanel)_inputCategoryPanelScene.Instantiate();
            categoryHeader.Init(category);
            _inputBindingsContainer.AddChild(categoryHeader);
        }
    }

}
