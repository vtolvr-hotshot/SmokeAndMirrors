using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Harmony;

using UnityEngine;
using UnityEngine.Events;

using VTNetworking;

namespace SmokeAndMirrors
{
    public class Main : VTOLMOD
    {
        public static bool SideMirrorsEnabled { get; private set; } = true;
        public static bool StopwatchEnabled { get; private set; } = true;
        public static Dictionary<string, GameObject> Prefabs { get; private set; } = new Dictionary<string, GameObject>();

        private static VTOLMOD instance;

        private event UnityAction OnAssetsLoaded;

        public static string[] resourceNames = new string[] {
                "fa26_smokeSystemWhite",
                "fa26_smokeSystemRed",
                "fa26_smokeSystemGreen",
                "fa26_smokeSystemBlue"
        };

        private static string[] assetNames = new string[] {
                "fa26_smokeSystemWhite",
                "fa26_smokeSystemRed",
                "fa26_smokeSystemGreen",
                "fa26_smokeSystemBlue",
                "SmokeIndicator",
                "SmokePanel",
                "SideMirror",
                "Stopwatch"
        };

        // Static logging methods for convenience. Calls the VTOLMOD logging functions,
        // which prefix the log message with the mod name.
        public static new void Log(object message)
        {
            instance.Log(message);
        }

        public static new void LogError(object message)
        {
            instance.LogError(message);
        }

        public static new void LogWarning(object message)
        {
            instance.LogWarning(message);
        }

        public override void ModLoaded()
        {
            if (instance != null)
            {
                LogError("Mod already loaded!");
                return;
            }

            instance = this;

            HarmonyInstance harmony = HarmonyInstance.Create("Hotshot.SmokeAndMirrors");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            base.ModLoaded();

            VTOLAPI.CreateSettingsMenu(CreateSettings());
            OnAssetsLoaded += RegisterHPEquippable;
            OnAssetsLoaded += ModifyVehiclePrefab;
            StartCoroutine(LoadAssetBundleAsync());
        }

        private Settings CreateSettings()
        {
            Settings settings = new Settings(this);
            settings.CreateCustomLabel("AirshowModSettings");

            settings.CreateBoolSetting(
                "Enable side mirrors?",
                value => { SideMirrorsEnabled = value; },
                SideMirrorsEnabled
                );

            settings.CreateBoolSetting(
                "Enable stopwatch?",
                value => { StopwatchEnabled = value; },
                StopwatchEnabled
                );
            
            return settings;
        }

        private void RegisterHPEquippable()
        {

            foreach (string resourceName in resourceNames)
            {
                if (Prefabs.TryGetValue(resourceName, out var prefab))
                {
                    string resourcePath = $"HPEquips/AFighter/{resourceName}";
                    VTResources.RegisterOverriddenResource(resourcePath, prefab);
                    VTNetworkManager.RegisterOverrideResource(resourcePath, prefab);
                }
            }
        }

        private void ModifyVehiclePrefab()
        {
            string resourcePath = "vehicles/FA-26B";
            var fa26bPrefab = VTResources.Load<GameObject>(resourcePath);

            if (fa26bPrefab == null)
            {
                return;
            }

            bool prefabWasModified = false;

            if (SideMirrorsEnabled)
            {
                fa26bPrefab.AddComponent<SideMirrorLoader>();
                prefabWasModified = true;
            }

            if (StopwatchEnabled)
            {
                fa26bPrefab.AddComponent<StopwatchLoader>();
                prefabWasModified = true;
            }

            if (prefabWasModified)
            {
                VTResources.RegisterOverriddenResource(resourcePath, fa26bPrefab);
            }
        }

        private IEnumerator LoadAssetBundleAsync()
        {
            // Get the path to the mod folder
            string filepath = Path.Combine(ModFolder, "smokeandmirrors.assets");
            Log($"Loading {filepath}");

            // Create the asset bundle request
            AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(filepath);
            yield return bundleRequest;

            if (bundleRequest.assetBundle == null)
            {
                LogError($"Could not load {filepath}");
                yield break;
            }

            foreach (string assetName in assetNames)
            {
                Log($"Loading {assetName} prefab");

                // Load the prefab
                AssetBundleRequest request = bundleRequest.assetBundle.LoadAssetAsync($"{assetName}.prefab");
                yield return request;

                if (request.asset == null)
                {
                    LogError($"Could not load {assetName} prefab");
                    continue;
                }

                // Configure the prefab
                GameObject prefab = request.asset as GameObject;
                DontDestroyOnLoad(prefab);
                prefab.SetActive(false);
                Prefabs.Add(assetName, prefab);
            }

            if (OnAssetsLoaded != null) OnAssetsLoaded();

            yield break;
        }
    }
}