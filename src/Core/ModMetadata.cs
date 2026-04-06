using System.IO;
using System.Reflection;
using MegaCrit.Sts2.Core.Modding;

namespace ModManagerSettings.Core;

internal static class ModMetadata
{
    private static readonly PropertyInfo? IdProperty =
        typeof(Mod).GetProperty("id") ??
        typeof(Mod).GetProperty("Id");

    private static readonly FieldInfo? IdField =
        typeof(Mod).GetField("id") ??
        typeof(Mod).GetField("Id");

    private static readonly PropertyInfo? PckNameProperty =
        typeof(Mod).GetProperty("pckName") ??
        typeof(Mod).GetProperty("PckName");

    private static readonly FieldInfo? PckNameField =
        typeof(Mod).GetField("pckName") ??
        typeof(Mod).GetField("PckName");

    private static readonly PropertyInfo? PathProperty =
        typeof(Mod).GetProperty("path") ??
        typeof(Mod).GetProperty("Path");

    private static readonly FieldInfo? PathField =
        typeof(Mod).GetField("path") ??
        typeof(Mod).GetField("Path");

    private static readonly PropertyInfo? ManifestIdProperty =
        typeof(ModManifest).GetProperty("id") ??
        typeof(ModManifest).GetProperty("Id");

    private static readonly FieldInfo? ManifestIdField =
        typeof(ModManifest).GetField("id") ??
        typeof(ModManifest).GetField("Id");

    private static readonly PropertyInfo? ManifestPckNameProperty =
        typeof(ModManifest).GetProperty("pckName") ??
        typeof(ModManifest).GetProperty("PckName");

    private static readonly FieldInfo? ManifestPckNameField =
        typeof(ModManifest).GetField("pckName") ??
        typeof(ModManifest).GetField("PckName");

    public static string GetPckName(Mod? mod)
    {
        if (mod == null)
        {
            return string.Empty;
        }

        try
        {
            if (IdProperty?.GetValue(mod) is string idPropertyValue &&
                !string.IsNullOrWhiteSpace(idPropertyValue))
            {
                return idPropertyValue;
            }

            if (IdField?.GetValue(mod) is string idFieldValue &&
                !string.IsNullOrWhiteSpace(idFieldValue))
            {
                return idFieldValue;
            }

            if (PckNameProperty?.GetValue(mod) is string propertyValue &&
                !string.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue;
            }

            if (PckNameField?.GetValue(mod) is string fieldValue &&
                !string.IsNullOrWhiteSpace(fieldValue))
            {
                return fieldValue;
            }
        }
        catch
        {
            // Fall back to manifest and assembly metadata below.
        }

        if (TryReadManifestId(mod.manifest) is { Length: > 0 } manifestId)
        {
            return manifestId;
        }

        if (TryReadManifestPckName(mod.manifest) is { Length: > 0 } manifestPckName)
        {
            return manifestPckName;
        }

        if (TryReadPath(mod) is { Length: > 0 } pathId)
        {
            return pathId;
        }

        if (!string.IsNullOrWhiteSpace(mod.assembly?.GetName().Name))
        {
            return mod.assembly.GetName().Name!;
        }

        return mod.manifest?.name ?? string.Empty;
    }

    public static string GetDisplayName(Mod? mod)
    {
        if (!string.IsNullOrWhiteSpace(mod?.manifest?.name))
        {
            return mod.manifest.name!;
        }

        return GetPckName(mod);
    }

    private static string? TryReadManifestId(ModManifest? manifest)
    {
        if (manifest == null)
        {
            return null;
        }

        try
        {
            if (ManifestIdProperty?.GetValue(manifest) is string propertyValue &&
                !string.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue;
            }

            if (ManifestIdField?.GetValue(manifest) is string fieldValue &&
                !string.IsNullOrWhiteSpace(fieldValue))
            {
                return fieldValue;
            }
        }
        catch
        {
            // Ignore reflection failures and fall through to null.
        }

        return null;
    }

    private static string? TryReadManifestPckName(ModManifest? manifest)
    {
        if (manifest == null)
        {
            return null;
        }

        try
        {
            if (ManifestPckNameProperty?.GetValue(manifest) is string propertyValue &&
                !string.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue;
            }

            if (ManifestPckNameField?.GetValue(manifest) is string fieldValue &&
                !string.IsNullOrWhiteSpace(fieldValue))
            {
                return fieldValue;
            }
        }
        catch
        {
            // Ignore reflection failures and fall through to null.
        }

        return null;
    }

    private static string? TryReadPath(Mod mod)
    {
        try
        {
            string? rawPath = null;
            if (PathProperty?.GetValue(mod) is string propertyValue &&
                !string.IsNullOrWhiteSpace(propertyValue))
            {
                rawPath = propertyValue;
            }
            else if (PathField?.GetValue(mod) is string fieldValue &&
                     !string.IsNullOrWhiteSpace(fieldValue))
            {
                rawPath = fieldValue;
            }

            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return null;
            }

            var normalized = rawPath.Replace('\\', '/').TrimEnd('/');
            if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(".pck", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileNameWithoutExtension(normalized);
            }

            return Path.GetFileName(normalized);
        }
        catch
        {
            return null;
        }
    }
}
