using System;
using ModManagerSettings.Api;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;

internal sealed class TextSettingRow : SettingRowBase, IApplySettingRow, IResetSettingRow, IPersistableSettingRow
{
    private readonly string _modKey;
    private readonly ModSettingTextDefinition _definition;
    private readonly LineEdit _lineEdit;

    public TextSettingRow(string modKey, ModSettingTextDefinition definition) : base(definition.Key, definition.Label, definition.Description)
    {
        _modKey = modKey;
        _definition = definition;

        _lineEdit = new LineEdit
        {
            Name = "Input",
            Text = definition.GetCurrentValue?.Invoke() ?? definition.DefaultValue,
            PlaceholderText = definition.PlaceholderText,
            SelectAllOnFocus = true,
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var resetButton = CreateResetButton();
        resetButton.Pressed += ResetToDefault;

        InputRow.AddChild(_lineEdit);
        InputRow.AddChild(resetButton);
    }

    public bool ApplyPending()
    {
        if (_definition.OnApply == null)
        {
            return false;
        }

        try
        {
            var value = _lineEdit.Text;
            _definition.OnApply.Invoke(value);
            Log.Info($"[ModManagerSettings] Text applied: mod='{_modKey}' key='{_definition.Key}' value='{value}'.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Text apply callback failed for key '{_definition.Key}'. {ex}");
            return false;
        }
    }

    public void ResetToDefault()
    {
        _lineEdit.Text = _definition.DefaultValue;
    }

    public string SettingKey => _definition.Key;

    public string SerializeCurrentValue()
    {
        return _lineEdit.Text;
    }

    public bool TrySetSerializedValue(string value)
    {
        _lineEdit.Text = value;
        return true;
    }
}
