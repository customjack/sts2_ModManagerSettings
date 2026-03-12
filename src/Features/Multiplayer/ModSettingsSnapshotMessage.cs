using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace ModManagerSettings.Features.Multiplayer;

internal enum SettingWireType : byte
{
    Toggle = 0,
    Number = 1,
    Choice = 2,
    Text = 3,
    Color = 4
}

internal struct ModSettingWireValue : IPacketSerializable
{
    public string ModPckName;
    public string Key;
    public SettingWireType Type;
    public string Value;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(ModPckName ?? string.Empty);
        writer.WriteString(Key ?? string.Empty);
        writer.WriteByte((byte)Type);
        writer.WriteString(Value ?? string.Empty);
    }

    public void Deserialize(PacketReader reader)
    {
        ModPckName = reader.ReadString();
        Key = reader.ReadString();
        Type = (SettingWireType)reader.ReadByte();
        Value = reader.ReadString();
    }
}

internal struct ModSettingsSnapshotMessage : INetMessage, IPacketSerializable
{
    public List<ModSettingWireValue> Values;

    public bool ShouldBroadcast => false;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.VeryDebug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteList(Values ?? new List<ModSettingWireValue>());
    }

    public void Deserialize(PacketReader reader)
    {
        Values = reader.ReadList<ModSettingWireValue>();
    }
}
