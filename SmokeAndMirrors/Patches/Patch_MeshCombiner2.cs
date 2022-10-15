using System.Linq;

using Harmony;

using UnityEngine;

namespace SmokeAndMirrors.Patches
{
    [HarmonyPatch(typeof(MeshCombiner2), "Start")]
    class Patch_MeshCombiner2_Start
    {
        static bool Prefix(MeshCombiner2 __instance)
        {
            if (PilotSaveManager.currentVehicle.vehicleName.Equals("F/A-26B") &&
                __instance != null && __instance.gameObject.name == "cockpitStaticCombiner")
            {
                var cmsPanel = __instance.transform.parent.Find("DashCanvas/LeftDash/Countermeasures");

                var cmsPanelMeshes = new MeshFilter[]
                {
                    cmsPanel.Find("panelEnd (1)")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("panelEnd (1)/panelMidsection")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("FlareSwitch/m_radioSwitchBase")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("ChaffSwitch/m_radioSwitchBase")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("FlareSwitch/radioSwitchBase/switchParent/radioSwitch")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("ChaffSwitch/radioSwitchBase/switchParent/radioSwitch")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("FlareSwitch/CMIndicator")?.GetComponent<MeshFilter>(),
                    cmsPanel.Find("ChaffSwitch/CMIndicator (1)")?.GetComponent<MeshFilter>(),
                };

                // Remove the above meshes from the MeshCombiner, since we will need to activate and deactivate them
                // when we swap out the countermeasures panel for the smoke panel.
                __instance.meshFilters =
                    __instance.meshFilters.Where(value => !cmsPanelMeshes.Contains(value)).ToArray();

                Main.Log("Removed countermeasure panel objects from MeshCombiner2");
            }

            return true;
        }
    }
}
