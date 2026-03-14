using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal sealed partial class ModSettingsSubmenu
{
    private void UpdateScrollLayout()
    {
        if (_contentScroll == null || _nodeHost == null || !_rowsByPath.TryGetValue(_activePath, out var activeRows))
        {
            return;
        }

        var width = Math.Max(320f, _contentScroll.Size.X - 20f);
        activeRows.CustomMinimumSize = new Vector2(width, 0f);
        activeRows.Size = new Vector2(width, activeRows.Size.Y);
        var minSize = activeRows.GetCombinedMinimumSize();
        var height = Math.Max(0f, minSize.Y);
        activeRows.CustomMinimumSize = new Vector2(width, height);

        _nodeHost.CustomMinimumSize = new Vector2(width, height);

        Log.Info(
            $"[ModManagerSettings] Scroll layout updated: " +
            $"scroll_size=({_contentScroll.Size.X:F1},{_contentScroll.Size.Y:F1}), " +
            $"active_path='{_activePath}', " +
            $"path_min=({minSize.X:F1},{minSize.Y:F1}), " +
            $"path_custom_min=({activeRows.CustomMinimumSize.X:F1},{activeRows.CustomMinimumSize.Y:F1}).");
    }

    private void RefreshRootSizeToViewport()
    {
        var viewport = GetViewport();
        if (viewport == null)
        {
            return;
        }

        var visibleRect = viewport.GetVisibleRect();
        Position = Vector2.Zero;
        Size = visibleRect.Size;
        CustomMinimumSize = visibleRect.Size;
        ApplyShellMargins();
        Callable.From(UpdateScrollLayout).CallDeferred();
    }

    private void ApplyShellMargins()
    {
        if (_shell == null)
        {
            return;
        }

        var panelWidth = Size.X * PanelHorizontalPercent;
        var panelHeight = Size.Y * PanelVerticalPercent;
        var horizontalMargin = Math.Max(8f, (Size.X - panelWidth) * 0.5f);
        var verticalMargin = Math.Max(8f, (Size.Y - panelHeight) * 0.5f);

        _shell.AddThemeConstantOverride("margin_left", (int)horizontalMargin);
        _shell.AddThemeConstantOverride("margin_top", (int)verticalMargin);
        _shell.AddThemeConstantOverride("margin_right", (int)horizontalMargin);
        _shell.AddThemeConstantOverride("margin_bottom", (int)verticalMargin);

        Log.Info(
            $"[ModManagerSettings] Shell layout: root_size=({Size.X:F1},{Size.Y:F1}), " +
            $"margins=({horizontalMargin:F1},{verticalMargin:F1}).");
    }
}
