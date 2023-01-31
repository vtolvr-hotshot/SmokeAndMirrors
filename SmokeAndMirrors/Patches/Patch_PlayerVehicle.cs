

using Harmony;

using UnityEngine;

namespace SmokeAndMirrors.Patches
{
    [HarmonyPatch(typeof(PlayerVehicle), nameof(PlayerVehicle.GetEquipPrefab))]
    class Patch_PlayerVehicle_GetEquipPrefab
    {
        static void Postfix(PlayerVehicle __instance, string equipName, ref GameObject __result)
        {
            if (__result == null)
            {
                if (Main.Prefabs.TryGetValue(equipName, out GameObject prefab))
                {
                    __result = prefab;
                }
            }
        }
    }
}
