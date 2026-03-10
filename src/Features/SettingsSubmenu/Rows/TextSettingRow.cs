using System;
using ModManagerSettings.Api;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.SettingsSubmenu.Rows;

internal sealed class TextSettingRow : SettingRowBase, IApplySettingRow
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

        InputRow.AddChild(_lineEdit);
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
}
