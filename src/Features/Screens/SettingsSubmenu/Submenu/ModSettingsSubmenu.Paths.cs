using System;
using System.Collections.Generic;
using System.Linq;
using ModManagerSettings.Api;
using Godot;

namespace ModManagerSettings.Features.Screens.SettingsSubmenu;

internal sealed partial class ModSettingsSubmenu
{
    private void BuildPathExplorer()
    {
        if (_nodeHost == null || _pathTree == null)
        {
            return;
        }

        ClearContainer(_nodeHost);
        _rowsByPath.Clear();
        _treeItemsByPath.Clear();
        _pathOrder.Clear();

        EnsurePathContainer(DefaultPath);
        EnsurePathContainer(MetaPath);
        EnsurePathContainer(OtherPath);

        RenderMetaRows();
        RenderSettingRows();
        RenderExtraRows();

        BuildPathTree();

        if (!_rowsByPath.ContainsKey(_activePath))
        {
            _activePath = DefaultPath;
        }

        SetActivePath(_activePath);
    }

    private void BuildPathTree()
    {
        if (_pathTree == null)
        {
            return;
        }

        _pathTree.Clear();
        _treeItemsByPath.Clear();

        var root = _pathTree.CreateItem();
        var orderedPaths = BuildTreeOrder(_rowsByPath.Keys);

        foreach (var path in orderedPaths)
        {
            var segments = SplitPath(path);
            var runningPath = string.Empty;
            var parent = root;

            for (var i = 0; i < segments.Count; i++)
            {
                runningPath = string.IsNullOrEmpty(runningPath)
                    ? segments[i]
                    : runningPath + "/" + segments[i];

                if (_treeItemsByPath.TryGetValue(runningPath, out var existing))
                {
                    parent = existing;
                    continue;
                }

                var item = _pathTree.CreateItem(parent);
                item.SetText(0, segments[i]);
                item.SetMetadata(0, runningPath);
                _treeItemsByPath[runningPath] = item;
                parent = item;
            }
        }

        if (_treeItemsByPath.TryGetValue(_activePath, out var active))
        {
            active.Select(0);
        }
    }

    private static List<string> BuildTreeOrder(IEnumerable<string> paths)
    {
        var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var running = string.Empty;
            foreach (var segment in SplitPath(path))
            {
                running = string.IsNullOrEmpty(running) ? segment : running + "/" + segment;
                all.Add(running);
            }
        }

        return all
            .OrderBy(path => GetTopLevelSortKey(path))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetTopLevelSortKey(string path)
    {
        var top = SplitPath(path).FirstOrDefault() ?? string.Empty;
        if (top.Equals(DefaultPath, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (top.Equals(MetaPath, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (top.Equals(OtherPath, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }

    private void OnPathTreeItemSelected()
    {
        if (_pathTree?.GetSelected() is not TreeItem selected)
        {
            return;
        }

        var metadata = selected.GetMetadata(0);
        if (metadata.VariantType != Variant.Type.String)
        {
            return;
        }

        var path = metadata.AsString();
        SetActivePath(path);
    }

    private void SetActivePath(string path)
    {
        var normalized = NormalizePath(path);
        if (!_rowsByPath.ContainsKey(normalized))
        {
            normalized = DefaultPath;
        }

        _activePath = normalized;

        if (_nodeHeaderLabel != null)
        {
            _nodeHeaderLabel.Text = _activePath;
        }

        foreach (var (name, rows) in _rowsByPath)
        {
            rows.Visible = string.Equals(name, _activePath, StringComparison.OrdinalIgnoreCase);
        }

        if (_pathTree != null && _treeItemsByPath.TryGetValue(_activePath, out var item))
        {
            item.Select(0);
        }

        if (_resetPathButton != null && _rowsByPath.TryGetValue(_activePath, out var activeRows))
        {
            _resetPathButton.Disabled = CountResettableRows(activeRows) == 0;
        }

        Callable.From(UpdateScrollLayout).CallDeferred();
    }

    private VBoxContainer EnsurePathContainer(string path)
    {
        var normalized = NormalizePath(path);
        if (_rowsByPath.TryGetValue(normalized, out var existing))
        {
            return existing;
        }

        if (_nodeHost == null)
        {
            throw new InvalidOperationException("UI node host has not been built.");
        }

        var rows = new VBoxContainer
        {
            Name = normalized.Replace("/", "_") + "Rows",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            Visible = false
        };
        rows.AddThemeConstantOverride("separation", 8);
        _nodeHost.AddChild(rows);

        _rowsByPath[normalized] = rows;
        _pathOrder.Add(normalized);
        return rows;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return DefaultPath;
        }

        var parts = SplitPath(path);
        if (parts.Count == 0)
        {
            return DefaultPath;
        }

        return string.Join("/", parts);
    }

    private static List<string> SplitPath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<string> { DefaultPath };
        }

        return raw
            .Replace('\\', '/')
            .Replace('>', '/')
            .Replace('.', '/')
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static string PathFor(ModSettingDefinitionBase setting)
    {
        return NormalizePath(setting.Path);
    }
}
