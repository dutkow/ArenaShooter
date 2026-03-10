using Godot;
using Godot.Collections;
using System;

public partial class InputCategoryPanel : VBoxContainer
{
    [Export] Label _categoryLabel;
    [Export] Container _inputBindingsContainer;

    [Export] PackedScene _inputBindingSettingEntryScene;

    private InputCategory _category;

    private Dictionary<string, InputBindingSettingsEntry> _inputBindingSettingsEntries = new();

    public void Init(InputCategory inputCategory)
    {
        _category = inputCategory;

        _categoryLabel.Text = inputCategory.LocalizationKey.ToUpper();

        InputManager.Instance.DefaultInputMappingsRestored += OnDefaultInputMappingsRestored;


        PopulateInputBindingEntries(inputCategory);
    }

    public void ClearInputBindingEntries()
    {
        foreach(Node child in _inputBindingsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    public void PopulateInputBindingEntries(InputCategory inputCategory)
    {
        ClearInputBindingEntries();

        foreach (var inputAction in inputCategory.InputActions)
        {
            var newEntry = (InputBindingSettingsEntry)_inputBindingSettingEntryScene.Instantiate();
            newEntry.Init(inputAction);
            _inputBindingSettingsEntries[inputAction.Action] = newEntry;
            _inputBindingsContainer.AddChild(newEntry);
        }
    }

    public void OnDefaultInputMappingsRestored(InputMappingProfile inputMappingProfile)
    {
        foreach(var inputAction in _category.InputActions)
        {
            if(_inputBindingSettingsEntries.TryGetValue(inputAction.Action, out var newEntry))
            {
                newEntry.Init(inputAction);
            }
        }
    }
}
