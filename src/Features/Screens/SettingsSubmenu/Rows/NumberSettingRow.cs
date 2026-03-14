using System;
using System.Globalization;
using ModManagerSettings.Api;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;

internal sealed class NumberSettingRow : SettingRowBase, IApplySettingRow, IResetSettingRow, IPersistableSettingRow
{
    private readonly string _modKey;
    private readonly ModSettingNumberDefinition _definition;
    private readonly SpinBox _spin;

    public NumberSettingRow(string modKey, ModSettingNumberDefinition definition) : base(definition.Key, definition.Label, definition.Description)
    {
        _modKey = modKey;
        _definition = definition;

        _spin = new SpinBox
        {
            Name = "Input",
            MinValue = definition.MinValue,
            MaxValue = definition.MaxValue,
            Step = definition.Step,
            Value = definition.GetCurrentValue?.Invoke() ?? definition.DefaultValue,
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var resetButton = CreateResetButton();
        resetButton.Pressed += ResetToDefault;

        InputRow.AddChild(_spin);
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
            var value = _spin.Value;
            _definition.OnApply.Invoke(value);
            Log.Info($"[ModManagerSettings] Number applied: mod='{_modKey}' key='{_definition.Key}' value={value:F4}.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Number apply callback failed for key '{_definition.Key}'. {ex}");
            return false;
        }
    }

    public void ResetToDefault()
    {
        _spin.Value = _definition.DefaultValue;
    }

    public string SettingKey => _definition.Key;

    public string SerializeCurrentValue()
    {
        return _spin.Value.ToString("R", CultureInfo.InvariantCulture);
    }

    public bool TrySetSerializedValue(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        _spin.Value = Math.Clamp(parsed, _definition.MinValue, _definition.MaxValue);
        return true;
    }
}
