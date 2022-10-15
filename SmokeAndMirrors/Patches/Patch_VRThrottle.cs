using System;

using Harmony;

using UnityEngine;
using UnityEngine.Events;

namespace SmokeAndMirrors
{
    // This class is meant to be added to VRThrottle and provides listeners for the second thumb button
    // (which VRThrottle doesn't care about) and a function to disable or reenable the countermeasure binds.
    [RequireComponent(typeof(VRThrottle))]
    [DisallowMultipleComponent]
    class VRThrottlePatch : MonoBehaviour
    {
#pragma warning disable CS0649
        public UnityAction OnMenuButtonDown;
        public UnityAction OnMenuButtonUp;
        public UnityAction OnSecondButtonDown;
        public UnityAction OnSecondButtonUp;
#pragma warning restore CS0649
        public bool sButtonIsDown = false;

        private VRThrottle throttle;

        private void Awake()
        {
            throttle = GetComponent<VRThrottle>();
        }

        public void DisableCountermeasureBinds()
        {
            Main.Log("Disabling VRThrottle countermeasure binds");

            try
            {
                throttle.OnMenuButtonDown.SetPersistentListenerState(0, UnityEventCallState.Off);
                throttle.OnMenuButtonUp.SetPersistentListenerState(0, UnityEventCallState.Off);
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not disable VRThrottle countermeasure binds!");
            }
        }

        public void EnableCountermeasureBinds()
        {
            Main.Log("Reenabling VRThrottle countermeasure binds");

            try
            {
                throttle.OnMenuButtonDown.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                throttle.OnMenuButtonUp.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not reenable VRThrottle countermeasure binds!");
            }
        }
    }
}

namespace SmokeAndMirrors.Patches
{
    // Add a VRThrottlePatch component
    [HarmonyPatch(typeof(VRThrottle), "Awake")]
    class Patch_VRThrottle_Awake
    {
        static void Postfix(VRThrottle __instance)
        {
            __instance.gameObject.AddComponent<VRThrottlePatch>();
        }
    }

    // Add references to the forward thumb button events to VRThrottlePatch, for ease of use
    [HarmonyPatch(typeof(VRThrottle), "Start")]
    class Patch_VRThrottle_Start
    {
        static void Postfix(VRThrottle __instance)
        {
            var remoteOnly = Traverse.Create(__instance).Field("remoteOnly").GetValue<bool>();

            if (__instance == null || remoteOnly)
            {
                return;
            }

            var throttlePatch = __instance.gameObject.GetComponent<VRThrottlePatch>();

            __instance.OnMenuButtonDown.AddListener(
                () =>
                {
                    if (throttlePatch.OnMenuButtonDown != null) throttlePatch.OnMenuButtonDown();
                }
             );

            __instance.OnMenuButtonUp.AddListener(
                () =>
                {
                    if (throttlePatch.OnMenuButtonUp != null) throttlePatch.OnMenuButtonUp();
                }
            );
        }
    }

    // Invoke the OnSecondButtonUp event if it is being pressed while the hand is taken off the throttle
    [HarmonyPatch(typeof(VRThrottle), "I_OnStopInteraction")]
    class Patch_VRThrottle_I_OnStopInteraction
    {
        static void Postfix(VRThrottle __instance)
        {
            var remoteOnly = Traverse.Create(__instance).Field("remoteOnly").GetValue<bool>();

            if (__instance == null || remoteOnly)
            {
                return;
            }

            var throttlePatch = __instance.gameObject.GetComponent<VRThrottlePatch>();

            if ((__instance.sendEvents && !remoteOnly) || __instance.alwaysSendButtonEvents)
            {
                if (throttlePatch.sButtonIsDown && throttlePatch.OnSecondButtonUp != null)
                {
                    throttlePatch.OnSecondButtonUp();
                }
            }

            throttlePatch.sButtonIsDown = false;
        }
    }

    // Invoke event listeners for the aft thumb button
    [HarmonyPatch(typeof(VRThrottle), "UpdateButtonEvents")]
    class ThrottleUpdateButtonEventsPatch
    {
        static void Postfix(VRThrottle __instance)
        {
            var remoteOnly = Traverse.Create(__instance).Field("remoteOnly").GetValue<bool>();

            if (__instance == null || remoteOnly)
            {
                return;
            }

            var controller = Traverse.Create(__instance).Field("controller").GetValue<VRHandController>();

            if ((!__instance.sendEvents && !__instance.alwaysSendButtonEvents) || !controller)
            {
                return;
            }

            var throttlePatch = __instance.gameObject.GetComponent<VRThrottlePatch>();

            if (controller.GetSecondButtonDown())
            {
                throttlePatch.sButtonIsDown = true;

                if (throttlePatch.OnSecondButtonDown != null) throttlePatch.OnSecondButtonDown();
            }

            if (controller.GetSecondButtonUp())
            {
                throttlePatch.sButtonIsDown = false;

                if (throttlePatch.OnSecondButtonUp != null) throttlePatch.OnSecondButtonUp();
            }
        }
    }
}
