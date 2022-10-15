using System;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SmokeAndMirrors
{
    [DisallowMultipleComponent]
    class SmokePanel : MonoBehaviour
    {
        public Transform CountermeasuresPanel { get; set; }
        public event UnityAction<bool> OnSetSwitchState;

        private Text smokeCountText;

        private void Awake()
        {
            ConfigurePanel();
            ConfigureLabels();
            ConfigureArmSwitch();
            ConfigureQuantityIndicator();

            var lever = GetComponentInChildren<VRLever>();
            lever.OnSetState.AddListener(SetSwitchState);
        }

        private void OnDestroy()
        {
            var lever = GetComponentInChildren<VRLever>();
            lever.OnSetState.RemoveListener(SetSwitchState);
        }

        public void SetArmed(bool shouldDisplayBeOn)
        {
            if (smokeCountText != null)
            {
                smokeCountText.enabled = shouldDisplayBeOn;
            }
        }

        public void SetQuantity(float quantity, float percent)
        {
            smokeCountText.text = Mathf.Max(Mathf.RoundToInt(quantity), 0).ToString();

            if (percent < 0.02f)
            {
                smokeCountText.color = Color.red;
            }
            else if (percent < 0.1f)
            {
                smokeCountText.color = Color.yellow;
            }
            else
            {
                smokeCountText.color = Color.green;
            }

            if (!smokeCountText.enabled)
            {
                smokeCountText.enabled = true;
            }
        }

        private void SetSwitchState(int state)
        {
            if (OnSetSwitchState != null) OnSetSwitchState(state > 0);
        }

        private void ConfigurePanel()
        {
            try
            {
                var smokePanelTop = transform.Find("panelTop");
                var smokePanelBottom = transform.Find("panelBottom");
                var cmsPanelTop = CountermeasuresPanel.Find("panelEnd (1)");

                CopyMeshAndMaterial(smokePanelTop, cmsPanelTop);
                CopyMeshAndMaterial(smokePanelBottom, cmsPanelTop);

                var smokePanelMiddle = transform.Find("panelMiddle");
                var cmsPanelMiddle = CountermeasuresPanel.Find("panelEnd (1)/panelMidsection");

                CopyMeshAndMaterial(smokePanelMiddle, cmsPanelMiddle);
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not configure panel for {transform}");
            }
        }

        private void ConfigureLabels()
        {
            try
            {
                var cmsLabel = CountermeasuresPanel.Find("cmsLabel");

                var textMaterial = cmsLabel.GetComponent<MeshRenderer>().sharedMaterial;
                var textFont = cmsLabel.GetComponent<VTText>().font;

                ConfigLabel(transform.Find("smokeLabel"));
                ConfigLabel(transform.Find("SmokeArmSwitch/arm"));
                ConfigLabel(transform.Find("SmokeArmSwitch/off"));
                ConfigLabel(transform.Find("Indicators/qty"));

                void ConfigLabel(Transform label)
                {
                    label.GetComponent<MeshFilter>().mesh = new Mesh();
                    label.GetComponent<MeshRenderer>().material = textMaterial;
                    label.GetComponent<VTText>().font = textFont;
                }
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not configure labels for {transform}");
            }
        }

        private void ConfigureArmSwitch()
        {
            try
            {
                var smokeSwitchBase = transform.Find("SmokeArmSwitch/radioSwitchBase");
                var cmsSwitchBase = CountermeasuresPanel.Find("FlareSwitch/m_radioSwitchBase");

                CopyMeshAndMaterial(smokeSwitchBase, cmsSwitchBase);

                var smokeSwitch = smokeSwitchBase.Find("switchParent/radioSwitch");
                var cmsSwitch = CountermeasuresPanel.Find("FlareSwitch/radioSwitchBase/switchParent/radioSwitch");

                CopyMeshAndMaterial(smokeSwitch, cmsSwitch);

                var smokeInteractable = smokeSwitch.Find("SmokeInteractable");
                var cmsInteractable = cmsSwitch.Find("FlareInteractable");

                var smokeAudioSource = smokeInteractable.GetComponent<AudioSource>();

                smokeInteractable.GetComponent<VRInteractable>().poseBounds = cmsInteractable.GetComponent<VRInteractable>().poseBounds;
                smokeAudioSource.clip = cmsInteractable.GetComponent<AudioSource>().clip;
                smokeAudioSource.outputAudioMixerGroup = cmsInteractable.GetComponent<AudioSource>().outputAudioMixerGroup;
                smokeInteractable.GetComponent<VRLever>().OnSetState.AddListener(i => smokeAudioSource.PlayOneShot(smokeAudioSource.clip));

            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not configure arm switch for {transform}");
            }
        }

        private void ConfigureQuantityIndicator()
        {
            try
            {
                var quantityIndicator = transform.Find("Indicators/QuantityIndicator");
                var cmsIndicator = CountermeasuresPanel.Find("FlareSwitch/CMIndicator");

                CopyMeshAndMaterial(quantityIndicator, cmsIndicator);

                var countText = quantityIndicator.Find("Image/SmokeCountText");
                var cmsCountText = cmsIndicator.Find("Image/FlareCountText");

                smokeCountText = countText.GetComponent<Text>();
                smokeCountText.font = cmsCountText.GetComponent<Text>().font;
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not configure quantity indicator for {transform}");
            }
        }

        private void CopyMeshAndMaterial(Transform to, Transform from)
        {
            try
            {
                to.GetComponent<MeshFilter>().mesh = from.GetComponent<MeshFilter>().mesh;
                to.GetComponent<MeshRenderer>().material = from.GetComponent<MeshRenderer>().material;
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not copy mesh and materials from {from} to {to}");
            }
        }
    }
}
