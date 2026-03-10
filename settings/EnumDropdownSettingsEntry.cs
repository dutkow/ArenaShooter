using Godot;
using System;

public partial class EnumDropdownSettingsEntry<TEnum> : DropdownSettingsEntry
    where TEnum : struct, Enum
{
    public Setting<TEnum> Setting;

    public void Init(Setting<TEnum> setting)
    {
        Setting = setting;

        PopulateItems();

        // Set initial selection based on Pending value
        var values = AssetUtils.GetEnumValues<TEnum>();
        int initialIndex = Array.IndexOf(values, Setting.Pending);
        if (initialIndex >= 0)
        {
            Dropdown.Select(initialIndex);
        }
    }

    public override void PopulateItems()
    {
        base.PopulateItems();
        Dropdown.Clear();

        var values = AssetUtils.GetEnumValues<TEnum>();
        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            Dropdown.AddItem(value.ToString());
            Dropdown.SetItemMetadata(i, Convert.ToInt32(value)); // metadata = enum int
        }
    }

    public override void OnItemSelected(long itemIndex)
    {
        base.OnItemSelected(itemIndex);

        // Convert dropdown index/metadata directly back to enum
        var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), (int)Dropdown.GetItemMetadata((int)itemIndex));
        Setting.Pending = enumValue; // triggers IMMEDIATE Apply if set, otherwise marks dirty

    }
}
