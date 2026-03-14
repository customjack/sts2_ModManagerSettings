using System;
using ModManagerSettings.Core;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal static class ModSettingsSubmenuController
{
    private const string SubmenuNodeName = "ModManagerSettingsSubmenu";

    public static void Open(NModMenuRow sourceRow, Mod mod)
    {
        try
        {
            var popupState = ProfileSettingsStore.RecordPopupOpened(mod.pckName);
            var savePath = ProfileSettingsStore.ResolveAbsolutePath();

            var stack = FindSubmenuStack(sourceRow);
            if (stack == null)
            {
                Log.Warn("[ModManagerSettings] Could not find NSubmenuStack from mod row; cannot open settings submenu.");
                return;
            }

            var submenu = stack.GetNodeOrNull<ModSettingsSubmenu>(SubmenuNodeName);
            if (submenu == null)
            {
                submenu = new ModSettingsSubmenu
                {
                    Name = SubmenuNodeName,
                    Visible = false,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                stack.AddChild(submenu);
                submenu.EnsureUiBuilt();
                Log.Info("[ModManagerSettings] Created reusable mod settings submenu instance.");
            }

            submenu.SetContext(mod, popupState, savePath);
            stack.Push(submenu);
            Log.Info($"[ModManagerSettings] Opened submenu for mod '{mod.pckName}'. save_path='{savePath}'.");
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Failed opening settings submenu for '{mod.pckName}'. {ex}");
        }
    }

    private static NSubmenuStack? FindSubmenuStack(Node start)
    {
        Node? current = start;
        while (current != null)
        {
            if (current is NSubmenuStack stack)
            {
                return stack;
            }

            current = current.GetParent();
        }

        return null;
    }
}
