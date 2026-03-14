using Godot;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal sealed partial class ModSettingsSubmenu
{
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

        var explorer = new HSplitContainer
        {
            Name = "Explorer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SplitOffset = 280
        };
        rootColumn.AddChild(explorer);

        var leftPanel = new PanelContainer
        {
            Name = "PathPanel",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(220f, 0f)
        };
        explorer.AddChild(leftPanel);

        var leftPad = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        leftPad.AddThemeConstantOverride("margin_left", 10);
        leftPad.AddThemeConstantOverride("margin_top", 10);
        leftPad.AddThemeConstantOverride("margin_right", 10);
        leftPad.AddThemeConstantOverride("margin_bottom", 10);
        leftPanel.AddChild(leftPad);

        var leftColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        leftColumn.AddThemeConstantOverride("separation", 8);
        leftPad.AddChild(leftColumn);

        leftColumn.AddChild(new Label
        {
            Text = "Config Paths",
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = Colors.White
        });

        _pathTree = new Tree
        {
            Name = "PathTree",
            HideRoot = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Columns = 1,
            FocusMode = FocusModeEnum.All
        };
        _pathTree.ItemSelected += OnPathTreeItemSelected;
        leftColumn.AddChild(_pathTree);

        var rightPanel = new PanelContainer
        {
            Name = "NodePanel",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        explorer.AddChild(rightPanel);

        var rightPad = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        rightPad.AddThemeConstantOverride("margin_left", 12);
        rightPad.AddThemeConstantOverride("margin_top", 10);
        rightPad.AddThemeConstantOverride("margin_right", 12);
        rightPad.AddThemeConstantOverride("margin_bottom", 10);
        rightPanel.AddChild(rightPad);

        var rightColumn = new VBoxContainer
        {
            Name = "NodeColumn",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        rightColumn.AddThemeConstantOverride("separation", 8);
        rightPad.AddChild(rightColumn);

        _nodeHeaderLabel = new Label
        {
            Name = "NodeHeader",
            Text = DefaultPath,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = Colors.White
        };
        rightColumn.AddChild(_nodeHeaderLabel);

        _contentScroll = new ScrollContainer
        {
            Name = "ContentScroll",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            FollowFocus = true,
            CustomMinimumSize = new Vector2(0f, MinScrollHeight)
        };
        rightColumn.AddChild(_contentScroll);
        _contentScroll.Connect(Control.SignalName.Resized, Callable.From(UpdateScrollLayout));

        _nodeHost = new VBoxContainer
        {
            Name = "NodeHost",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        _nodeHost.AddThemeConstantOverride("separation", 10);
        _contentScroll.AddChild(_nodeHost);

        var actionRow = new HBoxContainer
        {
            Name = "ActionRow",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        actionRow.AddThemeConstantOverride("separation", 10);
        rootColumn.AddChild(actionRow);

        _resetPathButton = new Button
        {
            Name = "ResetPathButton",
            Text = "Reset Path to Default",
            FocusMode = FocusModeEnum.All,
            CustomMinimumSize = new Vector2(180f, 30f)
        };
        _resetPathButton.Pressed += OnResetPathPressed;
        actionRow.AddChild(_resetPathButton);

        _defaultsButton = new Button
        {
            Name = "DefaultsButton",
            Text = "Reset All Settings to Default",
            FocusMode = FocusModeEnum.All,
            Visible = false,
            CustomMinimumSize = new Vector2(260f, 30f)
        };
        _defaultsButton.Pressed += OnResetAllPressed;
        actionRow.AddChild(_defaultsButton);

        actionRow.AddChild(new Control { Name = "Spacer", SizeFlagsHorizontal = SizeFlags.ExpandFill });

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

        _resetAllConfirmDialog = new ConfirmationDialog
        {
            Name = "ResetAllConfirmDialog",
            Title = "Reset All Settings",
            DialogText = "Reset all settings in this mod to defaults? This cannot be undone."
        };
        _resetAllConfirmDialog.Confirmed += OnResetAllConfirmed;
        AddChild(_resetAllConfirmDialog);
    }
}
