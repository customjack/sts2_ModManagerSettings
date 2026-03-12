using System;
using System.Collections.Generic;
using System.Globalization;
using ModManagerSettings.Api;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ModManagerSettings.Features.Multiplayer;

internal static class ModSettingsSyncService
{
    private sealed record BaselineValue(SettingWireType Type, string Value);

    private static readonly object SyncLock = new();
    private static readonly Dictionary<INetGameService, MessageHandlerDelegate<ModSettingsSnapshotMessage>> SnapshotHandlers = new();
    private static readonly Dictionary<INetGameService, Action<NetErrorInfo>> DisconnectHandlers = new();
    private static readonly Dictionary<string, Dictionary<string, BaselineValue>> ClientBaseline = new(StringComparer.OrdinalIgnoreCase);

    private static bool _clientOverridesActive;

    public static bool IsSessionLocked
    {
        get
        {
            lock (SyncLock)
            {
                foreach (var netService in SnapshotHandlers.Keys)
                {
                    if (netService.IsConnected && IsMultiplayer(netService.Type))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    public static void EnsureAttached(INetGameService netService, string source)
    {
        lock (SyncLock)
        {
            if (SnapshotHandlers.ContainsKey(netService))
            {
                return;
            }

            MessageHandlerDelegate<ModSettingsSnapshotMessage> snapshotHandler = (message, senderId) =>
            {
                HandleSnapshot(netService, message, senderId);
            };

            Action<NetErrorInfo> disconnectedHandler = _ =>
            {
                OnDisconnected(netService);
            };

            SnapshotHandlers[netService] = snapshotHandler;
            DisconnectHandlers[netService] = disconnectedHandler;
            netService.RegisterMessageHandler(snapshotHandler);
            netService.Disconnected += disconnectedHandler;
        }

        Log.Info($"[ModManagerSettings] Multiplayer sync attached to {netService.GetType().Name} from '{source}'.");
    }

    public static void SendSnapshotTo(INetGameService netService, ulong targetPlayerId, string source)
    {
        if (netService.Type != NetGameType.Host || !netService.IsConnected)
        {
            return;
        }

        EnsureAttached(netService, source);

        var values = BuildSnapshot();
        var message = new ModSettingsSnapshotMessage
        {
            Values = values
        };

        netService.SendMessage(message, targetPlayerId);
        Log.Info($"[ModManagerSettings] Sent settings snapshot to player {targetPlayerId} from '{source}'. entries={values.Count}.");
    }

    private static void HandleSnapshot(INetGameService netService, ModSettingsSnapshotMessage message, ulong senderId)
    {
        if (netService.Type != NetGameType.Client)
        {
            return;
        }

        var values = message.Values ?? new List<ModSettingWireValue>();
        if (values.Count == 0)
        {
            Log.Info($"[ModManagerSettings] Received empty settings snapshot from {senderId}.");
            return;
        }

        lock (SyncLock)
        {
            CaptureClientBaseline(values);
            ApplySnapshotValues(values, "[ModManagerSettings] Applied host snapshot");
            _clientOverridesActive = true;
        }

        Log.Info($"[ModManagerSettings] Applied host settings snapshot from {senderId}. entries={values.Count}.");
    }

    private static void OnDisconnected(INetGameService netService)
    {
        lock (SyncLock)
        {
            if (netService.Type == NetGameType.Client && _clientOverridesActive)
            {
                RestoreClientBaseline();
            }
        }
    }

    private static void CaptureClientBaseline(IEnumerable<ModSettingWireValue> incomingValues)
    {
        foreach (var value in incomingValues)
        {
            if (!TryReadCurrentValue(value.ModPckName, value.Key, value.Type, out var current))
            {
                continue;
            }

            if (!ClientBaseline.TryGetValue(value.ModPckName, out var modValues))
            {
                modValues = new Dictionary<string, BaselineValue>(StringComparer.OrdinalIgnoreCase);
                ClientBaseline[value.ModPckName] = modValues;
            }

            if (!modValues.ContainsKey(value.Key))
            {
                modValues[value.Key] = new BaselineValue(value.Type, current);
            }
        }
    }

    private static void RestoreClientBaseline()
    {
        var toRestore = new List<ModSettingWireValue>();

        foreach (var modEntry in ClientBaseline)
        {
            foreach (var keyEntry in modEntry.Value)
            {
                toRestore.Add(new ModSettingWireValue
                {
                    ModPckName = modEntry.Key,
                    Key = keyEntry.Key,
                    Type = keyEntry.Value.Type,
                    Value = keyEntry.Value.Value
                });
            }
        }

        ApplySnapshotValues(toRestore, "[ModManagerSettings] Restored local settings");
        ClientBaseline.Clear();
        _clientOverridesActive = false;
        Log.Info($"[ModManagerSettings] Restored client local settings after multiplayer session. entries={toRestore.Count}.");
    }

    private static void ApplySnapshotValues(IEnumerable<ModSettingWireValue> values, string logPrefix)
    {
        var touchedMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var applied = 0;

        foreach (var value in values)
        {
            if (ApplyValue(value))
            {
                applied++;
                touchedMods.Add(value.ModPckName);
            }
        }

        foreach (var modKey in touchedMods)
        {
            if (!ModSettingsRegistry.TryGet(modKey, out var registration) || registration.OnApply == null)
            {
                continue;
            }

            try
            {
                registration.OnApply.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error($"[ModManagerSettings] Registration-level apply callback failed for '{modKey}' during snapshot apply. {ex}");
            }
        }

        Log.Info($"{logPrefix}. applied={applied}, touched_mods={touchedMods.Count}.");
    }

    private static bool ApplyValue(ModSettingWireValue value)
    {
        if (!ModSettingsRegistry.TryGet(value.ModPckName, out var registration))
        {
            return false;
        }

        switch (value.Type)
        {
            case SettingWireType.Toggle:
            {
                var def = FindByKey(registration.ToggleSettings, value.Key);
                if (def?.OnApply == null || !bool.TryParse(value.Value, out var parsed))
                {
                    return false;
                }

                def.OnApply.Invoke(parsed);
                return true;
            }
            case SettingWireType.Number:
            {
                var def = FindByKey(registration.NumberSettings, value.Key);
                if (def?.OnApply == null || !double.TryParse(value.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    return false;
                }

                def.OnApply.Invoke(parsed);
                return true;
            }
            case SettingWireType.Choice:
            {
                var def = FindByKey(registration.ChoiceSettings, value.Key);
                if (def?.OnApply == null)
                {
                    return false;
                }

                def.OnApply.Invoke(value.Value);
                return true;
            }
            case SettingWireType.Text:
            {
                var def = FindByKey(registration.TextSettings, value.Key);
                if (def?.OnApply == null)
                {
                    return false;
                }

                def.OnApply.Invoke(value.Value);
                return true;
            }
            case SettingWireType.Color:
            {
                var def = FindByKey(registration.ColorSettings, value.Key);
                if (def?.OnApply == null)
                {
                    return false;
                }

                def.OnApply.Invoke(value.Value);
                return true;
            }
            default:
                return false;
        }
    }

    private static List<ModSettingWireValue> BuildSnapshot()
    {
        var values = new List<ModSettingWireValue>();

        foreach (var registration in ModSettingsRegistry.GetAll())
        {
            foreach (var def in registration.ToggleSettings)
            {
                AddCurrentValue(values, registration.ModPckName, def.Key, SettingWireType.Toggle, def.GetCurrentValue?.Invoke() ?? def.DefaultValue);
            }

            foreach (var def in registration.NumberSettings)
            {
                AddCurrentValue(values, registration.ModPckName, def.Key, SettingWireType.Number, def.GetCurrentValue?.Invoke() ?? def.DefaultValue);
            }

            foreach (var def in registration.ChoiceSettings)
            {
                AddCurrentValue(values, registration.ModPckName, def.Key, SettingWireType.Choice, def.GetCurrentValue?.Invoke() ?? def.DefaultValue);
            }

            foreach (var def in registration.TextSettings)
            {
                AddCurrentValue(values, registration.ModPckName, def.Key, SettingWireType.Text, def.GetCurrentValue?.Invoke() ?? def.DefaultValue);
            }

            foreach (var def in registration.ColorSettings)
            {
                AddCurrentValue(values, registration.ModPckName, def.Key, SettingWireType.Color, def.GetCurrentValue?.Invoke() ?? def.DefaultValue);
            }
        }

        return values;
    }

    private static void AddCurrentValue(List<ModSettingWireValue> values, string modPckName, string key, SettingWireType type, bool value)
    {
        values.Add(new ModSettingWireValue
        {
            ModPckName = modPckName,
            Key = key,
            Type = type,
            Value = value.ToString()
        });
    }

    private static void AddCurrentValue(List<ModSettingWireValue> values, string modPckName, string key, SettingWireType type, double value)
    {
        values.Add(new ModSettingWireValue
        {
            ModPckName = modPckName,
            Key = key,
            Type = type,
            Value = value.ToString("R", CultureInfo.InvariantCulture)
        });
    }

    private static void AddCurrentValue(List<ModSettingWireValue> values, string modPckName, string key, SettingWireType type, string value)
    {
        values.Add(new ModSettingWireValue
        {
            ModPckName = modPckName,
            Key = key,
            Type = type,
            Value = value ?? string.Empty
        });
    }

    private static bool TryReadCurrentValue(string modPckName, string key, SettingWireType type, out string value)
    {
        value = string.Empty;
        if (!ModSettingsRegistry.TryGet(modPckName, out var registration))
        {
            return false;
        }

        switch (type)
        {
            case SettingWireType.Toggle:
            {
                var def = FindByKey(registration.ToggleSettings, key);
                if (def == null)
                {
                    return false;
                }

                value = (def.GetCurrentValue?.Invoke() ?? def.DefaultValue).ToString();
                return true;
            }
            case SettingWireType.Number:
            {
                var def = FindByKey(registration.NumberSettings, key);
                if (def == null)
                {
                    return false;
                }

                value = (def.GetCurrentValue?.Invoke() ?? def.DefaultValue).ToString("R", CultureInfo.InvariantCulture);
                return true;
            }
            case SettingWireType.Choice:
            {
                var def = FindByKey(registration.ChoiceSettings, key);
                if (def == null)
                {
                    return false;
                }

                value = def.GetCurrentValue?.Invoke() ?? def.DefaultValue;
                return true;
            }
            case SettingWireType.Text:
            {
                var def = FindByKey(registration.TextSettings, key);
                if (def == null)
                {
                    return false;
                }

                value = def.GetCurrentValue?.Invoke() ?? def.DefaultValue;
                return true;
            }
            case SettingWireType.Color:
            {
                var def = FindByKey(registration.ColorSettings, key);
                if (def == null)
                {
                    return false;
                }

                value = def.GetCurrentValue?.Invoke() ?? def.DefaultValue;
                return true;
            }
            default:
                return false;
        }
    }

    private static T? FindByKey<T>(IReadOnlyList<T> definitions, string key) where T : ModSettingDefinitionBase
    {
        foreach (var definition in definitions)
        {
            if (string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    private static bool IsMultiplayer(NetGameType type)
    {
        return type == NetGameType.Host || type == NetGameType.Client;
    }
}
