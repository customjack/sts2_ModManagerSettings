using System;
using System.Collections.Generic;
using System.Linq;
using ModManagerSettings.Core;
using ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;
using Godot;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal sealed partial class ModSettingsSubmenu
{
    private void RenderMetaRows()
    {
        if (_targetMod == null)
        {
            return;
        }

        var rows = EnsurePathContainer(MetaPath);
        AddInfoCard(rows, "Mod Key", _targetMod.pckName);
        AddInfoCard(rows, "Display Name", _targetMod.manifest?.name ?? _targetMod.pckName);
        AddInfoCard(rows, "Author", _targetMod.manifest?.author ?? "Unknown");
        AddInfoCard(rows, "Version", _targetMod.manifest?.version ?? "Unknown");

        if (_currentRegistration != null)
        {
            AddInfoCard(rows, "Provider", _currentRegistration.DisplayName);
            if (!string.IsNullOrWhiteSpace(_currentRegistration.Description))
            {
                AddInfoCard(rows, "Provider Notes", _currentRegistration.Description);
            }
        }
        else
        {
            AddInfoCard(rows, "Provider", "No provider registered for this mod.");
        }
    }

    private void RenderSettingRows()
    {
        if (_currentRegistration == null || _targetMod == null)
        {
            AddInfoCard(EnsurePathContainer(DefaultPath), "No Settings", "No settings rows found for this mod.");
            return;
        }

        var persistedValues = ProfileSettingsStore.GetPersistedSettingsForMod(_targetMod.pckName);

        foreach (var toggle in _currentRegistration.ToggleSettings)
        {
            var row = new ToggleSettingRow(_targetMod.pckName, toggle);
            TryApplyPersistedValue(row, persistedValues);
            EnsurePathContainer(PathFor(toggle)).AddChild(row);
        }

        foreach (var number in _currentRegistration.NumberSettings)
        {
            var row = new NumberSettingRow(_targetMod.pckName, number);
            TryApplyPersistedValue(row, persistedValues);
            EnsurePathContainer(PathFor(number)).AddChild(row);
        }

        foreach (var choice in _currentRegistration.ChoiceSettings)
        {
            var row = new ChoiceSettingRow(_targetMod.pckName, choice);
            TryApplyPersistedValue(row, persistedValues);
            EnsurePathContainer(PathFor(choice)).AddChild(row);
        }

        foreach (var text in _currentRegistration.TextSettings)
        {
            var row = new TextSettingRow(_targetMod.pckName, text);
            TryApplyPersistedValue(row, persistedValues);
            EnsurePathContainer(PathFor(text)).AddChild(row);
        }

        foreach (var color in _currentRegistration.ColorSettings)
        {
            var row = new ColorSettingRow(_targetMod.pckName, color);
            TryApplyPersistedValue(row, persistedValues);
            EnsurePathContainer(PathFor(color)).AddChild(row);
        }

        var hasAnySettings = _rowsByPath
            .Where(pair =>
                !pair.Key.Equals(MetaPath, StringComparison.OrdinalIgnoreCase) &&
                !pair.Key.Equals(OtherPath, StringComparison.OrdinalIgnoreCase))
            .Any(pair => pair.Value.GetChildCount() > 0);

        if (!hasAnySettings)
        {
            AddInfoCard(EnsurePathContainer(DefaultPath), "No Rows", "Provider exists, but it registered zero rows.");
        }
    }

    private void RenderExtraRows()
    {
        if (_targetMod == null)
        {
            return;
        }

        var rows = EnsurePathContainer(OtherPath);
        AddInfoCard(rows, "Open Count", _popupState.PopupOpenCount.ToString());
        AddInfoCard(rows, "Last Opened (UTC ms)", _popupState.LastOpenedUtcUnixMs.ToString());
        AddInfoCard(rows, "Save Path", _savePath);

        if (_currentRegistration != null)
        {
            AddInfoCard(
                rows,
                "Registered Input Types",
                $"toggles={_currentRegistration.ToggleSettings.Count}, " +
                $"numbers={_currentRegistration.NumberSettings.Count}, " +
                $"choices={_currentRegistration.ChoiceSettings.Count}, " +
                $"text={_currentRegistration.TextSettings.Count}, " +
                $"colors={_currentRegistration.ColorSettings.Count}");
        }
    }

    private static void AddInfoCard(VBoxContainer container, string title, string value)
    {
        var card = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var pad = new MarginContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        pad.AddThemeConstantOverride("margin_left", 12);
        pad.AddThemeConstantOverride("margin_top", 10);
        pad.AddThemeConstantOverride("margin_right", 12);
        pad.AddThemeConstantOverride("margin_bottom", 10);
        card.AddChild(pad);

        var col = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        col.AddThemeConstantOverride("separation", 4);
        pad.AddChild(col);

        col.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Modulate = Colors.White
        });

        col.AddChild(new Label
        {
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Modulate = new Color(0.84f, 0.87f, 0.93f, 1f)
        });

        container.AddChild(card);
    }

    private static void ClearContainer(Control container)
    {
        foreach (Node child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private string ResolveExplorerDescription()
    {
        var defaultText = "Directory-style config explorer. Select a path on the left to edit that group.";
        if (_currentRegistration == null || string.IsNullOrWhiteSpace(_currentRegistration.ExplorerDescription))
        {
            return defaultText;
        }

        return _currentRegistration.ExplorerDescription.Trim();
    }
}
