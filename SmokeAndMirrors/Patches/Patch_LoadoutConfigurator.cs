using UnityEngine;

using Harmony;
using System.Collections.Generic;
using System.Collections;
using VTOLVR.Multiplayer;

namespace SmokeAndMirrors.Patches
{
    // Heavily inspired by NotBDArmory and CustomWeaponBase.
    // https://github.com/Temperz87/NotBDArmory
    // https://github.com/DankuwOs/CustomWeaponBase
    [HarmonyPatch(typeof(LoadoutConfigurator), nameof(LoadoutConfigurator.Initialize))]
    class Patch_LoadoutConfigurator_Initialize
    {
        static void Postfix(LoadoutConfigurator __instance)
        {
            if (__instance == null)
            {
                return;
            }

            Traverse traverse = Traverse.Create(__instance);

            Dictionary<string, EqInfo> unlockedWeaponPrefabs =
                traverse.Field("unlockedWeaponPrefabs").GetValue<Dictionary<string, EqInfo>>();

            string resourceName = "fa26_smokeSystem";

            // Add the smoke system to the F/A-26B configurator
            if (PilotSaveManager.currentVehicle.vehicleName.Equals("F/A-26B") &&
                Main.Prefabs.TryGetValue(resourceName, out var smokeSystemPrefab))
            {
                Main.Log("Adding smoke system to configurator");
                GameObject weaponPrefab = Object.Instantiate(smokeSystemPrefab);
                weaponPrefab.name = resourceName;
                EqInfo weaponInfo = new EqInfo(
                    weaponPrefab,
                    $"{PilotSaveManager.currentVehicle.equipsResourcePath}/{resourceName}"
                    );
                weaponPrefab.SetActive(false);

                unlockedWeaponPrefabs.Add(resourceName, weaponInfo);
                __instance.availableEquipStrings.Add(resourceName);
            }

            traverse.Field("unlockedWeaponPrefabs").SetValue(unlockedWeaponPrefabs);
            traverse.Field("allWeaponPrefabs").SetValue(unlockedWeaponPrefabs);
        }
    }

    // This is because EqInfo is a struct, so EqInfo.GetInstantiated() can't be patched...
    [HarmonyPatch(typeof(LoadoutConfigurator), nameof(LoadoutConfigurator.AttachImmediate))]
    class Patch_LoadoutConfigurator_Attach
    {
        static bool Prefix(LoadoutConfigurator __instance, string weaponName, int hpIdx)
        {
            if (weaponName.Equals("fa26_smokeSystem") && __instance != null)
            {
                AttachImmediateCustom(__instance, weaponName, hpIdx);
                return false;
            }

            return true;
        }

        private static void AttachImmediateCustom(LoadoutConfigurator __instance, string weaponName, int hpIdx)
        {
            Traverse traverse = Traverse.Create(__instance);
            var allWeaponPrefabs = traverse.Field("allWeaponPrefabs").GetValue<Dictionary<string, EqInfo>>();
            var hpTransforms = traverse.Field("hpTransforms").GetValue<Transform[]>();

            __instance.DetachImmediate(hpIdx);
            if (allWeaponPrefabs.ContainsKey(weaponName))
            {
                if (__instance.uiOnly)
                {
                    __instance.equips[hpIdx] = allWeaponPrefabs[weaponName].eq;
                    __instance.equips[hpIdx].OnConfigAttach(__instance);
                }
                else
                {
                    Transform transform = GetInstantiatedCustom(weaponName).transform;
                    __instance.equips[hpIdx] = transform.GetComponent<HPEquippable>();
                    transform.parent = hpTransforms[hpIdx];
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    __instance.equips[hpIdx].OnConfigAttach(__instance);

                    var externalHardpoints = VTOLAPI.GetPlayersVehicleGameObject()?.GetComponent<ExternalOptionalHardpoints>();
                    if (externalHardpoints != null)
                    {
                        Traverse.Create(externalHardpoints).Method("Wm_OnWeaponEquippedHPIdx").GetValue(hpIdx);
                    }
                }
            }
            __instance.UpdateNodes();
        }

        internal static GameObject GetInstantiatedCustom(string weaponName)
        {
            if (Main.Prefabs.TryGetValue(weaponName, out var weaponPrefab))
            {
                GameObject gameObject = Object.Instantiate(weaponPrefab);
                gameObject.name = weaponName;
                gameObject.SetActive(true);
                return gameObject;
            }

            throw new KeyNotFoundException($"Could not find {weaponName} prefab in Main.Prefabs");
        }
    }

    // This is because EqInfo is a struct, so EqInfo.GetInstantiated() can't be patched...
    [HarmonyPatch(typeof(LoadoutConfigurator), "AttachRoutine")]
    class Patch_LoadoutConfigurator_AttachRoutine
    {
        static bool Prefix(LoadoutConfigurator __instance, string weaponName, int hpIdx, ref IEnumerator __result)
        {
            if (weaponName.Equals("fa26_smokeSystem") && __instance != null)
            {
                __result = AttachRoutineCustom(__instance, weaponName, hpIdx);

                return false;
            }

            return true;
        }

        private static IEnumerator AttachRoutineCustom(LoadoutConfigurator __instance, string weaponName, int hpIdx)
        {
            Traverse traverse = Traverse.Create(__instance);
            var detachRoutines = traverse.Field("detachRoutines").GetValue<Coroutine[]>();
            var allWeaponPrefabs = traverse.Field("allWeaponPrefabs").GetValue<Dictionary<string, EqInfo>>();
            var hpTransforms = traverse.Field("hpTransforms").GetValue<Transform[]>();
            var iwbAttach = traverse.Field("iwbAttach").GetValue();
            var hpAudioSources = traverse.Field("hpAudioSources").GetValue<AudioSource[]>();
            var attachRoutines = traverse.Field("attachRoutines").GetValue<Coroutine[]>();

            if (detachRoutines[hpIdx] != null)
            {
                yield return detachRoutines[hpIdx];
            }
            if (!allWeaponPrefabs.ContainsKey(weaponName))
            {
                __instance.UpdateNodes();
                yield break;
            }
            GameObject instantiated = Patch_LoadoutConfigurator_Attach.GetInstantiatedCustom(weaponName);
            if (VTOLMPUtils.IsMultiplayer())
            {
                MissileLauncher[] componentsInChildrenImplementing = instantiated.GetComponentsInChildrenImplementing<MissileLauncher>(true);
                foreach (MissileLauncher missileLauncher in componentsInChildrenImplementing)
                {
                    if (missileLauncher.loadOnStart)
                    {
                        missileLauncher.LoadAllMissiles();
                    }
                }
            }
            Transform weaponTf = instantiated.transform;
            Transform hpTf = hpTransforms[hpIdx];
            InternalWeaponBay iwb = GetWeaponBayCustom(__instance, hpIdx);
            if ((bool)iwb)
            {
                iwb.RegisterOpenReq(iwbAttach);
            }
            __instance.equips[hpIdx] = weaponTf.GetComponent<HPEquippable>();
            __instance.equips[hpIdx].OnConfigAttach(__instance);

            var externalHardpoints = VTOLAPI.GetPlayersVehicleGameObject()?.GetComponent<ExternalOptionalHardpoints>();
            if (externalHardpoints != null)
            {
                Traverse.Create(externalHardpoints).Method("Wm_OnWeaponEquippedHPIdx").GetValue(hpIdx);
            }

            weaponTf.rotation = hpTf.rotation;
            Vector3 localPos = new Vector3(0f, -4f, 0f);
            weaponTf.position = hpTf.TransformPoint(localPos);
            __instance.UpdateNodes();
            Vector3 tgt = new Vector3(0f, 0f, 0.5f);
            if (hpIdx == 0 || (bool)iwb)
            {
                tgt = Vector3.zero;
            }
            while ((localPos - tgt).sqrMagnitude > 0.01f)
            {
                localPos = Vector3.Lerp(localPos, tgt, 5f * Time.deltaTime);
                weaponTf.position = hpTf.TransformPoint(localPos);
                yield return null;
            }
            weaponTf.parent = hpTf;
            weaponTf.localPosition = tgt;
            weaponTf.localRotation = Quaternion.identity;
            __instance.vehicleRb.AddForceAtPosition(Vector3.up * __instance.equipImpulse, __instance.wm.hardpointTransforms[hpIdx].position, ForceMode.Impulse);
            hpAudioSources[hpIdx].PlayOneShot(__instance.attachAudioClip);
            __instance.attachPs.transform.position = hpTf.position;
            __instance.attachPs.FireBurst();
            yield return new WaitForSeconds(0.2f);
            while (weaponTf.localPosition.sqrMagnitude > 0.001f)
            {
                weaponTf.localPosition = Vector3.MoveTowards(weaponTf.localPosition, Vector3.zero, 4f * Time.deltaTime);
                yield return null;
            }
            if ((bool)iwb)
            {
                iwb.UnregisterOpenReq(iwbAttach);
            }
            weaponTf.localPosition = Vector3.zero;
            __instance.UpdateNodes();
            attachRoutines[hpIdx] = null;
        }

        private static InternalWeaponBay GetWeaponBayCustom(LoadoutConfigurator __instance, int idx)
        {
            if (__instance.uiOnly)
            {
                return null;
            }
            for (int i = 0; i < __instance.wm.internalWeaponBays.Length; i++)
            {
                InternalWeaponBay internalWeaponBay = __instance.wm.internalWeaponBays[i];
                if (internalWeaponBay.hardpointIdx == idx)
                {
                    return internalWeaponBay;
                }
            }
            return null;
        }
    }

}
