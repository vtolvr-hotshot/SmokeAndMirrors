using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Harmony;

using UnityEngine;

using VTNetworking;

namespace SmokeAndMirrors
{
    public class Main : VTOLMOD
    {
        public static bool sideMirrorsEnabled = true;

        public static Dictionary<string, GameObject> Prefabs { get; private set; }

        private static VTOLMOD instance;

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
            StartCoroutine(LoadAssetBundleAsync());
        }

        private Settings CreateSettings()
        {
            Settings settings = new Settings(this);
            settings.CreateCustomLabel("AirshowModSettings");

            settings.CreateBoolSetting("Enable side mirrors?", SetSideMirrorsEnabled, sideMirrorsEnabled);

            return settings;

            void SetSideMirrorsEnabled(bool value) { sideMirrorsEnabled = value; }
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

            // Define the prefabs to be loaded
            string[] assetNames = new string[] { "fa26_smokeSystem", "SmokeIndicator", "SmokePanel", "SideMirror" };
            Prefabs = new Dictionary<string, GameObject>();

            foreach (string assetName in assetNames)
            {
                Log($"Loading {assetName} prefab");

                // Load the prefab
                AssetBundleRequest request = bundleRequest.assetBundle.LoadAssetAsync($"{assetName}.prefab");
                yield return request;

                if (request.asset == null)
                {
                    LogError($"Could not load {assetName} asset");
                    continue;
                }

                // Configure the prefab
                GameObject prefab = request.asset as GameObject;
                DontDestroyOnLoad(prefab);
                prefab.SetActive(false);
                Prefabs.Add(assetName, prefab);

                // Register the equippable prefab
                if (assetName.Equals("fa26_smokeSystem"))
                {
                    string resourcePath = $"HPEquips/AFighter/{assetName}";
                    VTResources.RegisterOverriddenResource(resourcePath, prefab);
                    VTNetworkManager.RegisterOverrideResource(resourcePath, prefab);
                }
            }

            yield break;
        }
    }
}