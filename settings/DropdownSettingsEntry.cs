using Godot;
using System;
using System.Collections.Generic;

public partial class DropdownSettingsEntry : SettingsEntry
{
    [Export] public OptionButton Dropdown;

    public override void _Ready()
    {
        base._Ready();

        PopulateItems();

        Dropdown.ItemSelected += OnItemSelected;
    }

    public virtual void PopulateItems()
    {
        Dropdown.Clear();
    }


    public virtual void OnItemSelected(long itemIndex) { }

}
