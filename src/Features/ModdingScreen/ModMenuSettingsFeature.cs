using System;
using HarmonyLib;
using ModManagerSettings.Features.SettingsSubmenu;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace ModManagerSettings.Features.ModdingScreen;

internal static class ModMenuSettingsFeature
{
    private const string SettingsButtonName = "ModManagerSettingsButton";
    private const float ButtonWidth = 70f;
    private const float ButtonHeight = 20f;
    private const float ButtonOuterPadding = 8f;
    private const float ButtonGapFromDecorations = 6f;
    private const float ButtonMinLeftX = 140f;
    private const float ReservedRightUiWidth = 118f;

    /// <summary>
    /// Adds a per-row settings button to the modding screen list.
    /// </summary>
    public static void Attach(NModMenuRow row)
    {
        if (row.Mod == null)
        {
            return;
        }

        if (row.FindChild(SettingsButtonName, recursive: false, owned: false) != null)
        {
            return;
        }

        var button = CreateButton(row, row.Mod);
        row.AddChild(button);

        // Re-run placement as row layout settles and whenever row width changes.
        row.Connect(Control.SignalName.Resized, Callable.From(() => PositionButton(row, button)));
        CallPositionDeferred(row, button);
        CallPositionDeferred(row, button);

        Log.Info($"[ModManagerSettings] Added settings button to mod row '{row.Mod.pckName}'.");
    }

    private static Button CreateButton(NModMenuRow row, Mod mod)
    {
        var button = new Button
        {
            Name = SettingsButtonName,
            Text = "Settings",
            TooltipText = "Open mod settings managed by ModManagerSettings.",
            FocusMode = Control.FocusModeEnum.All,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight),
            AnchorLeft = 0f,
            AnchorRight = 0f,
            AnchorTop = 0f,
            AnchorBottom = 0f
        };

        button.Pressed += () => ModSettingsSubmenuController.Open(row, mod);
        return button;
    }

    private static void CallPositionDeferred(NModMenuRow row, Button button)
    {
        Callable.From(() => PositionButton(row, button)).CallDeferred();
    }

    private static void PositionButton(NModMenuRow row, Control button)
    {
        var rightLimit = row.Size.X - ButtonOuterPadding - ReservedRightUiWidth;
        rightLimit = Math.Min(rightLimit, GetRightLimitFromObstacle(row, row.GetNodeOrNull<Control>("Tickbox")));
        rightLimit = Math.Min(rightLimit, GetRightLimitFromObstacle(row, row.GetNodeOrNull<Control>("PlatformIcon")));

        var maxX = Math.Max(0f, row.Size.X - ButtonWidth - ButtonOuterPadding);
        var minX = Math.Min(ButtonMinLeftX, maxX);
        var x = Math.Clamp(rightLimit - ButtonWidth, minX, maxX);
        var y = Math.Max(0f, (row.Size.Y - ButtonHeight) * 0.5f);

        button.Position = new Vector2(x, y);
        button.Size = new Vector2(ButtonWidth, ButtonHeight);
        button.CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight);
    }

    private static float GetRightLimitFromObstacle(Control row, Control? obstacle)
    {
        if (obstacle == null || !obstacle.Visible)
        {
            return float.MaxValue;
        }

        var rect = GetControlRect(obstacle);
        if (rect.Size.X <= 0f || rect.Position.X < row.Size.X * 0.5f)
        {
            return float.MaxValue;
        }

        return rect.Position.X - ButtonGapFromDecorations;
    }

    private static Rect2 GetControlRect(Control control)
    {
        var size = control.Size;
        if (size.X <= 0f)
        {
            size.X = Math.Max(size.X, control.CustomMinimumSize.X);
        }

        if (size.Y <= 0f)
        {
            size.Y = Math.Max(size.Y, control.CustomMinimumSize.Y);
        }

        return new Rect2(control.Position, size);
    }
}

[HarmonyPatch(typeof(NModMenuRow), nameof(NModMenuRow._Ready))]
internal static class NModMenuRowReadySettingsButtonPatch
{
    /// <summary>
    /// Adds a settings button after each mod menu row has created its default UI.
    /// </summary>
    public static void Postfix(NModMenuRow __instance)
    {
        try
        {
            ModMenuSettingsFeature.Attach(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Failed attaching settings button to mod row. {ex}");
        }
    }
}
