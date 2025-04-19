using BeatSaberMarkupLanguage.Components.Settings;

namespace OBSControl.Utilities;

internal static class BsmlExtensions
{
    public static void RefreshDropdown(this DropDownListSetting dropDownListSetting)
    {
        dropDownListSetting.UpdateChoices();
        dropDownListSetting.ReceiveValue();
    }
}