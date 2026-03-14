using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace ModManagerSettings.Core;

internal sealed class ProfileSettingsData
{
    public Dictionary<string, ModPopupState> Mods { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string LastOpenedMod { get; set; } = string.Empty;

    public Dictionary<string, Dictionary<string, string>> SettingsValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class ModPopupState
{
    public int PopupOpenCount { get; set; }

    public long LastOpenedUtcUnixMs { get; set; }
}

internal static class ProfileSettingsStore
{
    private const string RelativeSettingsPath = "saves/mod_manager_settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static readonly object LockObject = new();
    private static bool _loggedFallbackWarning;

    /// <summary>
    /// Resolves a user:// profile-scoped settings path.
    /// </summary>
    private static bool TryResolveUserPath(out string userPath, bool logOnFailure)
    {
        try
        {
            var raw = SaveManager.Instance.GetProfileScopedPath(RelativeSettingsPath);
            userPath = NormalizeSeparators(raw);
            return true;
        }
        catch (Exception ex)
        {
            var fallbackUserPath = "user://" + RelativeSettingsPath;
            userPath = fallbackUserPath;
            if (logOnFailure && !_loggedFallbackWarning)
            {
                _loggedFallbackWarning = true;
                Log.Warn($"[ModManagerSettings] Failed to resolve profile save path via SaveManager. Falling back to '{fallbackUserPath}'. {ex.Message}");
            }

            return false;
        }
    }

    private static string ResolveUserPath()
    {
        TryResolveUserPath(out var userPath, logOnFailure: true);
        return userPath;
    }

    /// <summary>
    /// Indicates whether SaveManager has initialized enough to resolve a profile-scoped path.
    /// </summary>
    public static bool IsProfileScopedPathReady()
    {
        return TryResolveUserPath(out _, logOnFailure: false);
    }

    /// <summary>
    /// Resolves the profile-scoped settings path that this mod writes to as an absolute filesystem path.
    /// </summary>
    public static string ResolveAbsolutePath()
    {
        var userOrAbsolutePath = NormalizeSeparators(ResolveUserPath());
        if (userOrAbsolutePath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectSettings.GlobalizePath(userOrAbsolutePath);
        }

        return userOrAbsolutePath;
    }

    /// <summary>
    /// Tracks each time the mod-settings popup is opened for a target mod and persists immediately.
    /// </summary>
    public static ModPopupState RecordPopupOpened(string modPckName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);

        lock (LockObject)
        {
            var data = LoadInternal();
            if (!data.Mods.TryGetValue(modPckName, out var state))
            {
                state = new ModPopupState();
                data.Mods[modPckName] = state;
            }

            state.PopupOpenCount += 1;
            state.LastOpenedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            data.LastOpenedMod = modPckName;

            if (SaveInternal(data))
            {
                Log.Info($"[ModManagerSettings] Saved popup telemetry for '{modPckName}' (count={state.PopupOpenCount}) at '{ResolveAbsolutePath()}'.");
            }
            else
            {
                Log.Warn($"[ModManagerSettings] Popup telemetry updated in memory for '{modPckName}', but persistence failed.");
            }

            return state;
        }
    }

    /// <summary>
    /// Returns persisted setting values for one mod key.
    /// </summary>
    public static Dictionary<string, string> GetPersistedSettingsForMod(string modPckName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);

        lock (LockObject)
        {
            var data = LoadInternal();
            if (!data.SettingsValues.TryGetValue(modPckName, out var values))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Persists a full snapshot of setting values for one mod key.
    /// </summary>
    public static void SavePersistedSettingsForMod(string modPckName, IReadOnlyDictionary<string, string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);
        ArgumentNullException.ThrowIfNull(values);

        lock (LockObject)
        {
            var data = LoadInternal();
            data.SettingsValues[modPckName] = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
            SaveInternal(data);
        }
    }

    /// <summary>
    /// Removes persisted settings snapshot for one mod key.
    /// </summary>
    public static void ClearPersistedSettingsForMod(string modPckName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);

        lock (LockObject)
        {
            var data = LoadInternal();
            if (data.SettingsValues.Remove(modPckName))
            {
                SaveInternal(data);
            }
        }
    }

    private static ProfileSettingsData LoadInternal()
    {
        var path = ResolveAbsolutePath();
        try
        {
            if (!File.Exists(path))
            {
                Log.Info($"[ModManagerSettings] No profile settings file found at '{path}'. Creating defaults in memory.");
                return new ProfileSettingsData();
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<ProfileSettingsData>(json, JsonOptions);
            if (data == null)
            {
                Log.Warn($"[ModManagerSettings] Settings file was empty or invalid at '{path}'. Using defaults.");
                return new ProfileSettingsData();
            }

            return data;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Failed reading profile settings from '{path}'. {ex}");
            return new ProfileSettingsData();
        }
    }

    private static bool SaveInternal(ProfileSettingsData data)
    {
        var path = ResolveAbsolutePath();
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModManagerSettings] Failed writing profile settings to '{path}'. {ex}");
            return false;
        }
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace('\\', '/');
    }
}
