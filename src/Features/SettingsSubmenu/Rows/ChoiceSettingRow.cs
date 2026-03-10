using System;
using ModManagerSettings.Api;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.SettingsSubmenu.Rows;

internal sealed class ChoiceSettingRow : SettingRowBase, IApplySettingRow
{
    private readonly string _modKey;
    private readonly ModSettingChoiceDefinition _definition;
    private readonly OptionButton _optionButton;

    public ChoiceSettingRow(string modKey, ModSettingChoiceDefinition definition) : base(definition.Key, definition.Label, definition.Description)
    {
        _modKey = modKey;
        _definition = definition;

        _optionButton = new OptionButton
        {
            Name = "Input",
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        for (var i = 0; i < definition.Options.Count; i++)
        {
            _optionButton.AddItem(definition.Options[i], i);
        }

        var currentValue = definition.GetCurrentValue?.Invoke() ?? definition.DefaultValue;
        var selected = FindChoiceIndex(definition, currentValue);
        if (selected >= 0)
        {
            _optionButton.Select(selected);
        }

        InputRow.AddChild(_optionButton);
    }

    private static int FindChoiceIndex(ModSettingChoiceDefinition definition, string value)
    {
        for (var i = 0; i < definition.Options.Count; i++)
        {
            if (string.Equals(definition.Options[i], value, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return definition.Options.Count > 0 ? 0 : -1;
    }

    public bool ApplyPending()
    {
        if (_definition.OnApply == null)
        {
            return false;
        }

        try
        {
            var idx = _optionButton.Selected;
            if (idx < 0 || idx >= _definition.Options.Count)
            {
                return false;
            }

            var value = _definition.Options[idx];
            _definition.OnApply.Invoke(value);
            Log.Info($"[ModManagerSettings] Choice applied: mod='{_modKey}' key='{_definition.Key}' value='{value}'.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Choice apply callback failed for key '{_definition.Key}'. {ex}");
            return false;
        }
    }
}
