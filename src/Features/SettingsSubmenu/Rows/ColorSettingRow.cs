using System;
using System.Globalization;
using ModManagerSettings.Api;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.SettingsSubmenu.Rows;

internal sealed class ColorSettingRow : SettingRowBase, IApplySettingRow
{
    private readonly string _modKey;
    private readonly ModSettingColorDefinition _definition;
    private readonly LineEdit _lineEdit;
    private readonly ColorRect _swatch;
    private readonly Label _statusLabel;

    public ColorSettingRow(string modKey, ModSettingColorDefinition definition) : base(definition.Key, definition.Label, definition.Description)
    {
        _modKey = modKey;
        _definition = definition;

        _lineEdit = new LineEdit
        {
            Name = "Input",
            Text = definition.GetCurrentValue?.Invoke() ?? definition.DefaultValue,
            PlaceholderText = definition.PlaceholderText,
            SelectAllOnFocus = true,
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        _swatch = new ColorRect
        {
            Name = "Swatch",
            CustomMinimumSize = new Vector2(56f, 28f),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            Color = Colors.Transparent
        };

        _statusLabel = new Label
        {
            Name = "Status",
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(66f, 0f),
            Modulate = Colors.White
        };

        _lineEdit.TextChanged += value =>
        {
            RefreshPreview(value);
        };

        RefreshPreview(_lineEdit.Text);

        InputRow.AddChild(_lineEdit);
        InputRow.AddChild(_swatch);
        InputRow.AddChild(_statusLabel);
    }

    public bool ApplyPending()
    {
        if (_definition.OnApply == null)
        {
            return false;
        }

        var value = _lineEdit.Text;
        if (!TryParseColor(value, out _, out _))
        {
            Log.Warn($"[ModManagerSettings] Color apply skipped for invalid value: mod='{_modKey}' key='{_definition.Key}' value='{value}'.");
            return false;
        }

        try
        {
            _definition.OnApply.Invoke(value);
            Log.Info($"[ModManagerSettings] Color applied: mod='{_modKey}' key='{_definition.Key}' value='{value}'.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Color apply callback failed for key '{_definition.Key}'. {ex}");
            return false;
        }
    }

    private void RefreshPreview(string value)
    {
        if (TryParseColor(value, out var color, out var normalized))
        {
            _swatch.Color = color;
            _statusLabel.Text = normalized;
            _statusLabel.Modulate = new Color(0.75f, 0.92f, 0.75f, 1f);
            return;
        }

        _swatch.Color = new Color(0.1f, 0.1f, 0.1f, 1f);
        _statusLabel.Text = "Invalid";
        _statusLabel.Modulate = new Color(0.95f, 0.58f, 0.58f, 1f);
    }

    private static bool TryParseColor(string raw, out Color color, out string normalizedRgba)
    {
        color = Colors.Transparent;
        normalizedRgba = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        if (text.StartsWith("#", StringComparison.Ordinal))
        {
            var hex = text[1..];
            if (hex.Length != 6 && hex.Length != 8)
            {
                return false;
            }

            if (!TryParseHexByte(hex, 0, out var r) || !TryParseHexByte(hex, 2, out var g) || !TryParseHexByte(hex, 4, out var b))
            {
                return false;
            }

            byte a = 255;
            if (hex.Length == 8 && !TryParseHexByte(hex, 6, out a))
            {
                return false;
            }

            color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            normalizedRgba = FormatRgba(color);
            return true;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 && parts.Length != 4)
        {
            return false;
        }

        if (!TryParseColorComponent(parts[0], out var rf) ||
            !TryParseColorComponent(parts[1], out var gf) ||
            !TryParseColorComponent(parts[2], out var bf))
        {
            return false;
        }

        var af = 1f;
        if (parts.Length == 4 && !TryParseColorComponent(parts[3], out af))
        {
            return false;
        }

        color = new Color(rf, gf, bf, af);
        normalizedRgba = FormatRgba(color);
        return true;
    }

    private static bool TryParseHexByte(string hex, int start, out byte value)
    {
        return byte.TryParse(
            hex.AsSpan(start, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool TryParseColorComponent(string value, out float normalized)
    {
        normalized = 0f;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (parsed > 1d)
        {
            parsed /= 255d;
        }

        parsed = Math.Clamp(parsed, 0d, 1d);
        normalized = (float)parsed;
        return true;
    }

    private static string FormatRgba(Color color)
    {
        var r = (int)Math.Round(color.R * 255f);
        var g = (int)Math.Round(color.G * 255f);
        var b = (int)Math.Round(color.B * 255f);
        var a = (int)Math.Round(color.A * 255f);
        return $"rgba({r},{g},{b},{a})";
    }
}
