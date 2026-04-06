using System;
using Godot;

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

    }
}
