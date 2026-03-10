using Godot;

namespace ModManagerSettings.Features.SettingsSubmenu.Rows;

/// <summary>
/// Base visual container for a single setting row.
/// Each row renders as a "card" (panel/div) with title, description, and input area.
/// </summary>
internal abstract class SettingRowBase : PanelContainer
{
    protected readonly VBoxContainer Content;
    protected readonly Label TitleLabel;
    protected readonly Label DescriptionLabel;
    protected readonly HBoxContainer InputRow;

    protected SettingRowBase(string key, string title, string description)
    {
        Name = $"SettingRow_{SanitizeNodeName(key)}";
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        CustomMinimumSize = new Vector2(0f, 84f);

        var padding = new MarginContainer
        {
            Name = "Padding",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        padding.AddThemeConstantOverride("margin_left", 12);
        padding.AddThemeConstantOverride("margin_top", 10);
        padding.AddThemeConstantOverride("margin_right", 12);
        padding.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(padding);

        Content = new VBoxContainer
        {
            Name = "Content",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        Content.AddThemeConstantOverride("separation", 6);
        padding.AddChild(Content);

        TitleLabel = new Label
        {
            Name = "Title",
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Modulate = Colors.White
        };
        Content.AddChild(TitleLabel);

        DescriptionLabel = new Label
        {
            Name = "Description",
            Text = description,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Modulate = new Color(0.84f, 0.87f, 0.93f, 1f),
            Visible = !string.IsNullOrWhiteSpace(description)
        };
        Content.AddChild(DescriptionLabel);

        InputRow = new HBoxContainer
        {
            Name = "InputRow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        Content.AddChild(InputRow);
    }

    private static string SanitizeNodeName(string value)
    {
        return value.Replace(" ", "_").Replace("/", "_");
    }
}
