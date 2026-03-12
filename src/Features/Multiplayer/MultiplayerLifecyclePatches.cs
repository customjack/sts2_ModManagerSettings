using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace ModManagerSettings.Features.Multiplayer;

[HarmonyPatch(typeof(NetClientGameService))]
internal static class NetClientGameServiceAttachPatch
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    public static void Postfix(NetClientGameService __instance)
    {
        ModSettingsSyncService.EnsureAttached(__instance, "NetClientGameService::.ctor");
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
internal static class NetHostGameServiceAttachPatch
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    public static void Postfix(NetHostGameService __instance)
    {
        ModSettingsSyncService.EnsureAttached(__instance, "NetHostGameService::.ctor");
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandleClientLobbyJoinRequestMessage")]
internal static class StartRunLobbyJoinSnapshotPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance, ulong senderId)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.Players.Exists(p => p.id == senderId))
        {
            return;
        }

        ModSettingsSyncService.SendSnapshotTo(__instance.NetService, senderId, "StartRunLobby join");
    }
}

[HarmonyPatch(typeof(LoadRunLobby), "HandleClientLoadJoinRequestMessage")]
internal static class LoadRunLobbyJoinSnapshotPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoadRunLobby __instance, ulong senderId)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.ConnectedPlayerIds.Contains(senderId))
        {
            return;
        }

        ModSettingsSyncService.SendSnapshotTo(__instance.NetService, senderId, "LoadRunLobby join");
    }
}

[HarmonyPatch(typeof(RunLobby), "HandleClientRejoinRequestMessage")]
internal static class RunLobbyRejoinSnapshotPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunLobby __instance, ulong senderId)
    {
        var netService = Traverse.Create(__instance).Field("_netService").GetValue<MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService>();
        if (netService == null || netService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.ConnectedPlayerIds.Contains(senderId))
        {
            return;
        }

        ModSettingsSyncService.SendSnapshotTo(netService, senderId, "RunLobby rejoin");
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandlePlayerReadyMessage")]
internal static class StartRunLobbyReadyResendPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance, ulong senderId)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.Players.Exists(p => p.id == senderId))
        {
            return;
        }

        ModSettingsSyncService.SendSnapshotTo(__instance.NetService, senderId, "StartRunLobby ready");
    }
}

[HarmonyPatch(typeof(LoadRunLobby), "HandlePlayerReadyMessage")]
internal static class LoadRunLobbyReadyResendPatch
{
    [HarmonyPostfix]
    public static void Postfix(LoadRunLobby __instance, ulong senderId)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.ConnectedPlayerIds.Contains(senderId))
        {
            return;
        }

        ModSettingsSyncService.SendSnapshotTo(__instance.NetService, senderId, "LoadRunLobby ready");
    }
}
