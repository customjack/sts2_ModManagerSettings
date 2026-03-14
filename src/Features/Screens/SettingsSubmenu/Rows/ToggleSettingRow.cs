using System;
using ModManagerSettings.Api;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;

internal sealed class ToggleSettingRow : SettingRowBase
    , IApplySettingRow
    , IResetSettingRow
    , IPersistableSettingRow
{
    private readonly string _modKey;
    private readonly ModSettingToggleDefinition _definition;
    private readonly CheckBox _checkbox;

    public ToggleSettingRow(string modKey, ModSettingToggleDefinition definition) : base(definition.Key, definition.Label, definition.Description)
    {
        _modKey = modKey;
        _definition = definition;

        _checkbox = new CheckBox
        {
            Name = "Input",
            Text = "Enabled",
            ButtonPressed = definition.GetCurrentValue?.Invoke() ?? definition.DefaultValue,
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(170f, 34f),
            Modulate = Colors.White
        };

        var stateLabel = new Label
        {
            Name = "StateLabel",
            Text = _checkbox.ButtonPressed ? "ON" : "OFF",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(40f, 0f),
            Modulate = new Color(0.96f, 0.86f, 0.38f, 1f)
        };

        _checkbox.Toggled += value => { stateLabel.Text = value ? "ON" : "OFF"; };

        var resetButton = CreateResetButton();
        resetButton.Pressed += ResetToDefault;

        InputRow.AddChild(_checkbox);
        InputRow.AddChild(stateLabel);
        InputRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
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
            var value = _checkbox.ButtonPressed;
            _definition.OnApply.Invoke(value);
            Log.Info($"[ModManagerSettings] Toggle applied: mod='{_modKey}' key='{_definition.Key}' value={value}.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Toggle apply callback failed for key '{_definition.Key}'. {ex}");
            return false;
        }
    }

    public void ResetToDefault()
    {
        _checkbox.ButtonPressed = _definition.DefaultValue;
    }

    public string SettingKey => _definition.Key;

    public string SerializeCurrentValue()
    {
        return _checkbox.ButtonPressed.ToString();
    }

    public bool TrySetSerializedValue(string value)
    {
        if (!bool.TryParse(value, out var parsed))
        {
            return false;
        }

        _checkbox.ButtonPressed = parsed;
        return true;
    }
}
