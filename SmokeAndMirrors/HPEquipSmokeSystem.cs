using System;

using UnityEngine;

namespace SmokeAndMirrors
{
    [DisallowMultipleComponent]
    class HPEquipSmokeSystem : HPEquippable
    {
#pragma warning disable CS0649
        [Tooltip("Cost per liter of smoke oil.")]
        public float perQuantityCost;

        public SmokeSystem smokeSystem;
        public SmokeSystemAnimator smokeSystemAnimator;
        public SmokeSystemParticles smokeSystemParticles;
#pragma warning restore CS0649

        private bool hasSmokeSystemBeenEnabled;
        private VRThrottlePatch throttlePatch;
        private SmokeIndicator smokeIndicator;
        private SmokePanel smokePanel;
        private Transform countermeasuresPanel;

        public bool IsRemote { get; set; }

        protected override void OnEquip()
        {
            base.OnEquip();

            throttlePatch = weaponManager.transform.root.GetComponentInChildren<VRThrottlePatch>();

            InitializeSmokeSystem();
            EnableSmokeSystemEventsAndUI();
        }

        private void OnDestroy()
        {
            DisableSmokeSystemEventsAndUI();
        }

        private void LateUpdate()
        {
            if (weaponManager != null && weaponManager.ui != null && weaponManager.ui.mfdPage.isOpen)
            {
                weaponManager.ui.UpdateDisplay();
            }
        }

        public override float GetCurrentPrice()
        {
            return unitCost + perQuantityCost * smokeSystem.CurrentOil;
        }

        public override float GetNewItemCost()
        {
            return unitCost + perQuantityCost * smokeSystem.startingOil;
        }

        public override int GetCount()
        {
            return Mathf.RoundToInt(smokeSystem.CurrentOil);
        }

        public override int GetMaxCount()
        {
            return Mathf.RoundToInt(smokeSystem.maxOil);
        }

        public override float GetEstimatedMass()
        {
            return smokeSystem.systemMass + smokeSystem.maxOil * smokeSystem.oilDensity;
        }

        public override void OnQuicksaveEquip(ConfigNode eqNode)
        {
            base.OnQuicksaveEquip(eqNode);
            eqNode.SetValue("normalizedSmokeOil", smokeSystem.NormalizedOil);
        }

        public override void OnQuickloadEquip(ConfigNode eqNode)
        {
            base.OnQuickloadEquip(eqNode);
            smokeSystem.NormalizedOil = ConfigNodeUtils.ParseFloat(eqNode.GetValue("normalizedSmokeOil"));
        }

        private void InitializeSmokeSystem()
        {
            Main.Log("Initializing smoke system");

            try
            {
                // Get references to the left engine and FlightInfo
                var engine = weaponManager.transform.Find("aFighter2/fa26-leftEngine").GetComponent<ModuleEngine>();
                var flightInfo = weaponManager.GetComponent<FlightInfo>();

                // Set references for the animator and particles
                smokeSystemAnimator.Engine = engine;
                smokeSystemParticles.Engine = engine;
                smokeSystemParticles.FlightInfo = flightInfo;

                // Setup cockpit elements and battery reference only for the local player
                if (!IsRemote)
                {
                    countermeasuresPanel = weaponManager.transform.Find("Local/DashCanvas/LeftDash/Countermeasures");
                    InstantiateSmokeIndicator();
                    InstantiateSmokePanel();
                    smokeSystem.Battery = weaponManager.transform.GetComponentInChildren<Battery>();
                }
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not initialize smoke system!");
            }
        }

        private void InstantiateSmokeIndicator()
        {
            if (smokeIndicator != null)
            {
                return;
            }

            Main.Log("Instantiating smoke indicator");

            try
            {
                // Get references to the dash and afterburner indicator
                var dash = weaponManager.transform.Find("Local/DashCanvas/Dash");
                var afterburnerIndicator = dash.Find("AfterburnerIndicator");

                // Instantiate the smoke indicator
                if (Main.Prefabs.TryGetValue("SmokeIndicator", out var smokeIndicatorPrefab))
                {
                    var smokeIndicatorObject = Instantiate(smokeIndicatorPrefab, dash);
                    smokeIndicatorObject.transform.localPosition = new Vector3(-386.3f, 201.7f, 54.8f);
                    smokeIndicatorObject.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
                    smokeIndicatorObject.transform.localScale = new Vector3(600.734f, 600.734f, 600.734f);

                    smokeIndicator = smokeIndicatorObject.GetComponent<SmokeIndicator>();
                    smokeIndicator.AfterburnerIndicator = afterburnerIndicator;
                }
                else
                {
                    Main.LogError("Could not find SmokeIndicator prefab!");
                }
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not initialize smoke indicator!");
            }
        }

        private void InstantiateSmokePanel()
        {
            if (smokePanel != null)
            {
                return;
            }

            Main.Log("Instantiating smoke panel");

            try
            {
                // Get a reference to the left dash
                var leftDash = weaponManager.transform.Find("Local/DashCanvas/LeftDash");

                // Instantiate the smoke panel
                if (Main.Prefabs.TryGetValue("SmokePanel", out var smokePanelPrefab))
                {
                    var smokePanelObject = Instantiate(smokePanelPrefab, leftDash);
                    smokePanelObject.transform.localPosition = countermeasuresPanel.localPosition;
                    smokePanelObject.transform.localRotation = countermeasuresPanel.localRotation;
                    smokePanelObject.transform.localScale = countermeasuresPanel.localScale;

                    smokePanel = smokePanelObject.GetComponent<SmokePanel>();
                    smokePanel.CountermeasuresPanel = countermeasuresPanel;
                }
                else
                {
                    Main.LogError("Could not find SmokePanel prefab!");
                }
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not initialize smoke panel!");
            }
        }

        private void EnableSmokeSystemEventsAndUI()
        {
            // Only enable events and UI for the local player
            if (IsRemote)
            {
                return;
            }

            // Subscribe listeners for smoke system, panel, and indicator
            smokeSystem.OnSetState += smokeIndicator.SetState;
            smokeSystem.OnSetArmed += smokePanel.SetArmed;
            smokeSystem.OnUpdateQuantity += smokePanel.SetQuantity;
            smokePanel.OnSetSwitchState += smokeSystem.SetArmSwitchState;

            // Bind throttle buttons to smoke, unbind countermeasures
            throttlePatch.OnMenuButtonDown += smokeSystem.SetToggleOn;
            throttlePatch.OnSecondButtonDown += smokeSystem.SetToggleOff;
            throttlePatch.DisableCountermeasureBinds();

            // Activate smoke objects, deactivate countermeasures panel
            smokeIndicator.gameObject.SetActive(true);
            smokePanel.gameObject.SetActive(true);
            countermeasuresPanel.gameObject.SetActive(false);

            // Flag to prevent disable method from running before enable
            hasSmokeSystemBeenEnabled = true;
        }

        private void DisableSmokeSystemEventsAndUI()
        {
            // Only disable events for the local player, and only if they've been enabled previously
            if (IsRemote || !hasSmokeSystemBeenEnabled)
            {
                return;
            }

            // Unsubscribe listeners for smoke system, panel, and indicator
            smokeSystem.OnSetState -= smokeIndicator.SetState;
            smokeSystem.OnSetArmed -= smokePanel.SetArmed;
            smokeSystem.OnUpdateQuantity -= smokePanel.SetQuantity;
            smokePanel.OnSetSwitchState -= smokeSystem.SetArmSwitchState;

            // Unbind throttle smoke buttons, rebind countermeasures
            throttlePatch.OnMenuButtonDown -= smokeSystem.SetToggleOn;
            throttlePatch.OnSecondButtonDown -= smokeSystem.SetToggleOff;
            throttlePatch.EnableCountermeasureBinds();

            // Deactivate smoke objects, re-activate countermeasures panel
            smokeIndicator.gameObject.SetActive(false);
            smokePanel.gameObject.SetActive(false);
            countermeasuresPanel.gameObject.SetActive(true);
        }
    }
}
;