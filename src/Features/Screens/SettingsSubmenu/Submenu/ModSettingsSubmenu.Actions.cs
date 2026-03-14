using System;
using System.Collections.Generic;
using ModManagerSettings.Api;
using ModManagerSettings.Core;
using ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal sealed partial class ModSettingsSubmenu
{
    private void OnResetPathPressed()
    {
        if (!_rowsByPath.TryGetValue(_activePath, out var rows))
        {
            return;
        }

        var resetCount = 0;
        foreach (Node child in rows.GetChildren())
        {
            if (child is IResetSettingRow resettable)
            {
                resettable.ResetToDefault();
                resetCount++;
            }
        }

        Log.Info($"[ModManagerSettings] Reset current path to defaults: mod='{_targetMod?.pckName}' path='{_activePath}' rows={resetCount}.");
    }

    private void OnResetAllPressed()
    {
        if (_targetMod == null)
        {
            return;
        }

        _resetAllConfirmDialog?.PopupCentered();
    }

    private void OnResetAllConfirmed()
    {
        if (_targetMod == null)
        {
            return;
        }

        try
        {
            if (_currentRegistration?.OnRestoreDefaults != null)
            {
                _currentRegistration.OnRestoreDefaults.Invoke();
                Log.Info($"[ModManagerSettings] Reset-all callback completed for '{_targetMod.pckName}'.");
            }
            else
            {
                var resetRows = ResetAllSettingRowsToDefaults();
                var appliedRows = ApplySettingRows();
                if (_currentRegistration?.OnApply != null)
                {
                    _currentRegistration.OnApply.Invoke();
                }

                Log.Info(
                    $"[ModManagerSettings] Reset-all fallback completed for '{_targetMod.pckName}'. " +
                    $"rows_reset={resetRows}, rows_applied={appliedRows}, registration_apply={_currentRegistration?.OnApply != null}.");
            }

            ProfileSettingsStore.ClearPersistedSettingsForMod(_targetMod.pckName);
            RefreshUi();
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Reset-all callback failed for '{_targetMod.pckName}'. {ex}");
        }
    }

    private static int CountResettableRows(VBoxContainer rows)
    {
        var count = 0;
        foreach (Node child in rows.GetChildren())
        {
            if (child is IResetSettingRow)
            {
                count++;
            }
        }

        return count;
    }

    private int CountAllResettableRows()
    {
        var count = 0;
        foreach (var rows in _rowsByPath.Values)
        {
            count += CountResettableRows(rows);
        }

        return count;
    }

    private int ResetAllSettingRowsToDefaults()
    {
        var reset = 0;
        foreach (var rows in _rowsByPath.Values)
        {
            foreach (Node child in rows.GetChildren())
            {
                if (child is IResetSettingRow resettable)
                {
                    resettable.ResetToDefault();
                    reset++;
                }
            }
        }

        return reset;
    }

    private void OnApplyPressed()
    {
        if (_targetMod == null)
        {
            return;
        }

        if (_currentRegistration == null)
        {
            Log.Info($"[ModManagerSettings] Apply pressed for '{_targetMod.pckName}', but no settings registration exists.");
            return;
        }

        var appliedRows = ApplySettingRows();

        try
        {
            if (_currentRegistration.OnApply != null)
            {
                _currentRegistration.OnApply.Invoke();
                Log.Info($"[ModManagerSettings] Registration-level Apply callback completed for '{_targetMod.pckName}'.");
            }

            if (appliedRows == 0 && _currentRegistration.OnApply == null)
            {
                Log.Info($"[ModManagerSettings] Apply pressed for '{_targetMod.pckName}', but no per-setting or registration apply callbacks are registered.");
            }
            else
            {
                Log.Info($"[ModManagerSettings] Apply completed for '{_targetMod.pckName}'. per_setting_callbacks={appliedRows}, has_registration_callback={_currentRegistration.OnApply != null}.");
            }

            PersistCurrentSettingRows();
            RefreshUi();
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Apply callback failed for '{_targetMod.pckName}'. {ex}");
        }
    }

    private void OnBackPressed()
    {
        _stack?.Pop();
    }

    private int ApplySettingRows()
    {
        if (_rowsByPath.Count == 0)
        {
            return 0;
        }

        var applied = 0;
        foreach (var rows in _rowsByPath.Values)
        {
            foreach (Node child in rows.GetChildren())
            {
                if (child is IApplySettingRow applySettingRow && applySettingRow.ApplyPending())
                {
                    applied++;
                }
            }
        }

        return applied;
    }

    private void PersistCurrentSettingRows()
    {
        if (_targetMod == null)
        {
            return;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rows in _rowsByPath.Values)
        {
            foreach (Node child in rows.GetChildren())
            {
                if (child is IPersistableSettingRow persistable)
                {
                    values[persistable.SettingKey] = persistable.SerializeCurrentValue();
                }
            }
        }

        ProfileSettingsStore.SavePersistedSettingsForMod(_targetMod.pckName, values);
        Log.Info($"[ModManagerSettings] Persisted {values.Count} setting values for '{_targetMod.pckName}'.");
    }

    private static void TryApplyPersistedValue(Control row, IReadOnlyDictionary<string, string> persistedValues)
    {
        if (row is not IPersistableSettingRow persistable)
        {
            return;
        }

        if (!persistedValues.TryGetValue(persistable.SettingKey, out var value))
        {
            return;
        }

        if (!persistable.TrySetSerializedValue(value))
        {
            Log.Warn($"[ModManagerSettings] Failed applying persisted value for key '{persistable.SettingKey}'.");
        }
    }

    private static bool HasAnyApplyCallbacks(ModSettingsRegistration registration)
    {
        if (registration.OnApply != null)
        {
            return true;
        }

        foreach (var toggle in registration.ToggleSettings)
        {
            if (toggle.OnApply != null)
            {
                return true;
            }
        }

        foreach (var number in registration.NumberSettings)
        {
            if (number.OnApply != null)
            {
                return true;
            }
        }

        foreach (var choice in registration.ChoiceSettings)
        {
            if (choice.OnApply != null)
            {
                return true;
            }
        }

        foreach (var text in registration.TextSettings)
        {
            if (text.OnApply != null)
            {
                return true;
            }
        }

        foreach (var color in registration.ColorSettings)
        {
            if (color.OnApply != null)
            {
                return true;
            }
        }

        return false;
    }
}
