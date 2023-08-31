using HarmonyLib;
using KSP.Game;
using KSP.Sim.impl;
// ReSharper disable InconsistentNaming

namespace KerbonautManager;

public class KerbalsVanishingPatch
{
    private static bool _isNewCampaign;

    [HarmonyPatch(typeof(VesselComponent), nameof(VesselComponent.RecoverVessel))]
    [HarmonyPrefix]
    public static void VesselComponent_RecoverVessel(VesselComponent __instance, IGGuid recoveryLocation)
    {
        var roster = __instance.Game.SessionManager.KerbalRosterManager;
        var kerbals = roster.GetAllKerbalsInVessel(__instance.GlobalId);

        foreach (var kerbal in kerbals)
        {
            roster.SetKerbalLocation(kerbal, roster.KSCGuid, IGGuid.Empty, -1);
        }
    }

    [HarmonyPatch(typeof(CreateCampaignMenu), "CreateNewCampaign")]
    [HarmonyPrefix]
    public static void CreateCampaignMenu_CreateNewCampaign()
    {
        _isNewCampaign = true;
    }

    [HarmonyPatch(typeof(PopulationComponent), nameof(PopulationComponent.OnUpdate))]
    [HarmonyPrefix]
    public static bool PopulationComponent_OnUpdate_Prefix()
    {
        return _isNewCampaign;
    }

    [HarmonyPatch(typeof(PopulationComponent), nameof(PopulationComponent.OnUpdate))]
    [HarmonyPostfix]
    public static void PopulationComponent_OnUpdate_Postfix(int ____currentKerbalCountTotal, int ____refillKerbalLimit)
    {
        if (_isNewCampaign && ____currentKerbalCountTotal >= ____refillKerbalLimit)
        {
            _isNewCampaign = false;
        }
    }
}