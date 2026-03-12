using System;
using ModManagerSettings.Api;
using ModManagerSettings.Core;
using ModManagerSettings.Features.SettingsSubmenu.Rows;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace ModManagerSettings.Features.SettingsSubmenu;

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

internal sealed class ModSettingsSubmenu : NSubmenu
{
    private const float PanelHorizontalPercent = 0.75f;
    private const float PanelVerticalPercent = 0.94f;
    private const float MinScrollHeight = 320f;

    private MarginContainer? _shell;
    private Label? _headerLabel;
    private Label? _descriptionLabel;
    private ScrollContainer? _contentScroll;
    private VBoxContainer? _sectionsContainer;
    private VBoxContainer? _metaRows;
    private VBoxContainer? _settingsRows;
    private VBoxContainer? _extraRows;
    private Button? _defaultsButton;
    private Button? _applyButton;
    private Button? _backButton;

    private Mod? _targetMod;
    private ModPopupState _popupState = new();
    private string _savePath = string.Empty;
    private ModSettingsRegistration? _currentRegistration;
    private bool _uiBuilt;

    protected override Control? InitialFocusedControl => _applyButton ?? _backButton;

    public override void _Ready()
    {
        EnsureUiBuilt();
        Connect(Control.SignalName.Resized, Callable.From(RefreshRootSizeToViewport));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent && keyEvent.Keycode == Key.Escape)
        {
            _stack?.Pop();
            GetViewport().SetInputAsHandled();
        }
    }

    public void EnsureUiBuilt()
    {
        if (_uiBuilt)
        {
            return;
        }

        _uiBuilt = true;
        BuildUi();
    }

    public void SetContext(Mod mod, ModPopupState popupState, string savePath)
    {
        _targetMod = mod;
        _popupState = popupState;
        _savePath = savePath;
        RefreshUi();
    }

    public override void OnSubmenuOpened()
    {
        RefreshRootSizeToViewport();
        RefreshUi();
        Callable.From(() => (_applyButton ?? _backButton)?.GrabFocus()).CallDeferred();
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var backdrop = new ColorRect
        {
            Name = "Backdrop",
            Color = new Color(0.02f, 0.03f, 0.05f, 0.86f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        _shell = new MarginContainer
        {
            Name = "Shell",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _shell.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_shell);
        ApplyShellMargins();

        var panel = new PanelContainer
        {
            Name = "Panel",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _shell.AddChild(panel);

        var panelPadding = new MarginContainer
        {
            Name = "PanelPadding",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panelPadding.AddThemeConstantOverride("margin_left", 18);
        panelPadding.AddThemeConstantOverride("margin_top", 14);
        panelPadding.AddThemeConstantOverride("margin_right", 18);
        panelPadding.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(panelPadding);

        var rootColumn = new VBoxContainer
        {
            Name = "RootColumn",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        rootColumn.AddThemeConstantOverride("separation", 10);
        panelPadding.AddChild(rootColumn);

        _headerLabel = new Label
        {
            Name = "Header",
            Text = "MOD SETTINGS",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = Colors.White
        };
        rootColumn.AddChild(_headerLabel);

        _descriptionLabel = new Label
        {
            Name = "Description",
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = new Color(0.84f, 0.87f, 0.93f, 1f)
        };
        rootColumn.AddChild(_descriptionLabel);

        _contentScroll = new ScrollContainer
        {
            Name = "ContentScroll",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            FollowFocus = true,
            CustomMinimumSize = new Vector2(0f, MinScrollHeight)
        };
        rootColumn.AddChild(_contentScroll);
        _contentScroll.Connect(Control.SignalName.Resized, Callable.From(UpdateScrollLayout));

        _sectionsContainer = new VBoxContainer
        {
            Name = "Sections",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        _sectionsContainer.AddThemeConstantOverride("separation", 12);
        _contentScroll.AddChild(_sectionsContainer);

        _metaRows = AddSection(_sectionsContainer, "Meta");
        _settingsRows = AddSection(_sectionsContainer, "Settings");
        _extraRows = AddSection(_sectionsContainer, "Other");

        var actionRow = new HBoxContainer
        {
            Name = "ActionRow",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        actionRow.AddThemeConstantOverride("separation", 10);
        rootColumn.AddChild(actionRow);

        _defaultsButton = new Button
        {
            Name = "DefaultsButton",
            Text = "Defaults",
            FocusMode = FocusModeEnum.All,
            Visible = false,
            CustomMinimumSize = new Vector2(120f, 30f)
        };
        _defaultsButton.Pressed += OnDefaultsPressed;
        actionRow.AddChild(_defaultsButton);

        var spacer = new Control
        {
            Name = "Spacer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        actionRow.AddChild(spacer);

        _applyButton = new Button
        {
            Name = "ApplyButton",
            Text = "Apply",
            FocusMode = FocusModeEnum.All,
            CustomMinimumSize = new Vector2(120f, 30f)
        };
        _applyButton.Pressed += OnApplyPressed;
        actionRow.AddChild(_applyButton);

        _backButton = new Button
        {
            Name = "BackButton",
            Text = "Back",
            FocusMode = FocusModeEnum.All,
            CustomMinimumSize = new Vector2(120f, 30f)
        };
        _backButton.Pressed += OnBackPressed;
        actionRow.AddChild(_backButton);
    }

    private static VBoxContainer AddSection(VBoxContainer parent, string name)
    {
        var title = new Label
        {
            Text = name,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = Colors.White
        };
        parent.AddChild(title);

        var rows = new VBoxContainer
        {
            Name = name + "Rows",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        rows.AddThemeConstantOverride("separation", 8);
        parent.AddChild(rows);
        return rows;
    }

    private void RefreshUi()
    {
        EnsureUiBuilt();

        if (_targetMod == null || _headerLabel == null || _descriptionLabel == null || _metaRows == null || _settingsRows == null || _extraRows == null || _defaultsButton == null || _applyButton == null)
        {
            return;
        }

        ClearRows(_metaRows);
        ClearRows(_settingsRows);
        ClearRows(_extraRows);

        _currentRegistration = ModSettingsRegistry.TryGet(_targetMod.pckName, out var registration)
            ? registration
            : null;

        _headerLabel.Text = $"MOD SETTINGS: {_targetMod.manifest?.name ?? _targetMod.pckName}";
        _descriptionLabel.Text = "Settings are grouped into sections. Each input is rendered as its own row object (card/div style).";

        RenderMetaRows();
        RenderSettingRows();
        RenderExtraRows();

        _defaultsButton.Visible = _currentRegistration?.OnRestoreDefaults != null;
        _defaultsButton.Disabled = _currentRegistration?.OnRestoreDefaults == null;
        _applyButton.Disabled = _currentRegistration == null || !HasAnyApplyCallbacks(_currentRegistration);
        ApplyShellMargins();
        Callable.From(UpdateScrollLayout).CallDeferred();

        Log.Info(
            $"[ModManagerSettings] Rendered sections for '{_targetMod.pckName}': " +
            $"meta_rows={_metaRows.GetChildCount()}, settings_rows={_settingsRows.GetChildCount()}, other_rows={_extraRows.GetChildCount()}.");
    }

    private void RenderMetaRows()
    {
        if (_targetMod == null || _metaRows == null)
        {
            return;
        }

        AddInfoCard(_metaRows, "Mod Key", _targetMod.pckName);
        AddInfoCard(_metaRows, "Display Name", _targetMod.manifest?.name ?? _targetMod.pckName);
        AddInfoCard(_metaRows, "Author", _targetMod.manifest?.author ?? "Unknown");
        AddInfoCard(_metaRows, "Version", _targetMod.manifest?.version ?? "Unknown");

        if (_currentRegistration != null)
        {
            AddInfoCard(_metaRows, "Provider", _currentRegistration.DisplayName);
            if (!string.IsNullOrWhiteSpace(_currentRegistration.Description))
            {
                AddInfoCard(_metaRows, "Provider Notes", _currentRegistration.Description);
            }
        }
        else
        {
            AddInfoCard(_metaRows, "Provider", "No provider registered for this mod.");
        }
    }

    private void RenderSettingRows()
    {
        if (_settingsRows == null)
        {
            return;
        }

        if (_currentRegistration == null || _targetMod == null)
        {
            AddInfoCard(_settingsRows, "No Settings", "No settings rows found for this mod.");
            return;
        }

        foreach (var toggle in _currentRegistration.ToggleSettings)
        {
            _settingsRows.AddChild(new ToggleSettingRow(_targetMod.pckName, toggle));
        }

        foreach (var number in _currentRegistration.NumberSettings)
        {
            _settingsRows.AddChild(new NumberSettingRow(_targetMod.pckName, number));
        }

        foreach (var choice in _currentRegistration.ChoiceSettings)
        {
            _settingsRows.AddChild(new ChoiceSettingRow(_targetMod.pckName, choice));
        }

        foreach (var text in _currentRegistration.TextSettings)
        {
            _settingsRows.AddChild(new TextSettingRow(_targetMod.pckName, text));
        }

        foreach (var color in _currentRegistration.ColorSettings)
        {
            _settingsRows.AddChild(new ColorSettingRow(_targetMod.pckName, color));
        }

        if (_settingsRows.GetChildCount() == 0)
        {
            AddInfoCard(_settingsRows, "No Rows", "Provider exists, but it registered zero rows.");
        }
    }

    private void RenderExtraRows()
    {
        if (_extraRows == null || _targetMod == null)
        {
            return;
        }

        AddInfoCard(_extraRows, "Open Count", _popupState.PopupOpenCount.ToString());
        AddInfoCard(_extraRows, "Last Opened (UTC ms)", _popupState.LastOpenedUtcUnixMs.ToString());
        AddInfoCard(_extraRows, "Save Path", _savePath);

        if (_currentRegistration != null)
        {
            AddInfoCard(
                _extraRows,
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

    private static void ClearRows(VBoxContainer container)
    {
        foreach (Node child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void OnDefaultsPressed()
    {
        if (_targetMod == null || _currentRegistration?.OnRestoreDefaults == null)
        {
            return;
        }

        try
        {
            _currentRegistration.OnRestoreDefaults.Invoke();
            Log.Info($"[ModManagerSettings] Defaults callback completed for '{_targetMod.pckName}'.");
            RefreshUi();
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Defaults callback failed for '{_targetMod.pckName}'. {ex}");
        }
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
        if (_settingsRows == null)
        {
            return 0;
        }

        var applied = 0;
        foreach (Node child in _settingsRows.GetChildren())
        {
            if (child is IApplySettingRow applySettingRow && applySettingRow.ApplyPending())
            {
                applied++;
            }
        }

        return applied;
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

    private void UpdateScrollLayout()
    {
        if (_contentScroll == null || _sectionsContainer == null)
        {
            return;
        }

        var width = Math.Max(320f, _contentScroll.Size.X - 20f);
        // Apply width first so wrapped labels can report a realistic vertical minimum.
        _sectionsContainer.CustomMinimumSize = new Vector2(width, 0f);
        _sectionsContainer.Size = new Vector2(width, _sectionsContainer.Size.Y);
        var minSize = _sectionsContainer.GetCombinedMinimumSize();
        var height = Math.Max(0f, minSize.Y);
        _sectionsContainer.CustomMinimumSize = new Vector2(width, height);

        Log.Info(
            $"[ModManagerSettings] Scroll layout updated: " +
            $"scroll_size=({_contentScroll.Size.X:F1},{_contentScroll.Size.Y:F1}), " +
            $"sections_min=({minSize.X:F1},{minSize.Y:F1}), " +
            $"sections_size=({_sectionsContainer.Size.X:F1},{_sectionsContainer.Size.Y:F1}), " +
            $"sections_custom_min=({_sectionsContainer.CustomMinimumSize.X:F1},{_sectionsContainer.CustomMinimumSize.Y:F1}).");
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
