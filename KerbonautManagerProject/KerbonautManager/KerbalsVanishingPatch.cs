using HarmonyLib;
using KSP.Sim.impl;

namespace KerbonautManager;

public class KerbalsVanishingPatch
{
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

    [HarmonyPatch(typeof(PopulationComponent), nameof(PopulationComponent.OnUpdate))]
    [HarmonyPrefix]
    public static bool PopulationComponent_OnUpdate()
    {
        return false;
    }
}