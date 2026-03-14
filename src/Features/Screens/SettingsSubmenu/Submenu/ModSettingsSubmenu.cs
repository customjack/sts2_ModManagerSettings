using System;
using System.Collections.Generic;
using System.Linq;
using ModManagerSettings.Api;
using ModManagerSettings.Core;
using ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal sealed partial class ModSettingsSubmenu : NSubmenu
{
    private const string DefaultPath = "Settings";
    private const string MetaPath = "Meta";
    private const string OtherPath = "Other";

    private const float PanelHorizontalPercent = 0.82f;
    private const float PanelVerticalPercent = 0.94f;
    private const float MinScrollHeight = 320f;

    private MarginContainer? _shell;
    private Label? _headerLabel;
    private Label? _descriptionLabel;
    private Tree? _pathTree;
    private Label? _nodeHeaderLabel;
    private ScrollContainer? _contentScroll;
    private VBoxContainer? _nodeHost;
    private Button? _resetPathButton;
    private Button? _defaultsButton;
    private Button? _applyButton;
    private Button? _backButton;
    private ConfirmationDialog? _resetAllConfirmDialog;

    private Mod? _targetMod;
    private ModPopupState _popupState = new();
    private string _savePath = string.Empty;
    private ModSettingsRegistration? _currentRegistration;

    private readonly Dictionary<string, VBoxContainer> _rowsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TreeItem> _treeItemsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _pathOrder = new();
    private string _activePath = DefaultPath;
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

    private void RefreshUi()
    {
        EnsureUiBuilt();

        if (_targetMod == null || _headerLabel == null || _descriptionLabel == null || _resetPathButton == null || _defaultsButton == null || _applyButton == null)
        {
            return;
        }

        _currentRegistration = ModSettingsRegistry.TryGet(_targetMod.pckName, out var registration)
            ? registration
            : null;

        _headerLabel.Text = $"MOD SETTINGS: {_targetMod.manifest?.name ?? _targetMod.pckName}";
        _descriptionLabel.Text = ResolveExplorerDescription();

        BuildPathExplorer();

        _resetPathButton.Disabled = !_rowsByPath.TryGetValue(_activePath, out var activeRows) || CountResettableRows(activeRows) == 0;
        _defaultsButton.Visible = true;
        _defaultsButton.Disabled = CountAllResettableRows() == 0 && _currentRegistration?.OnRestoreDefaults == null;
        _applyButton.Disabled = _currentRegistration == null || !HasAnyApplyCallbacks(_currentRegistration);

        ApplyShellMargins();
        Callable.From(UpdateScrollLayout).CallDeferred();

        var summary = string.Join(", ", _pathOrder.Select(path => $"{path}={_rowsByPath[path].GetChildCount()}"));
        Log.Info($"[ModManagerSettings] Rendered path explorer for '{_targetMod.pckName}': {summary}.");
    }
}
