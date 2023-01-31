using System;

using UnityEngine;
using UnityEngine.UI;

namespace SmokeAndMirrors
{
    class StopwatchLoader : MonoBehaviour
    {
        private void Awake()
        {
            Vector3 position = new Vector3(-82.50467f, -75.10004f, 25.79972f);
            Quaternion rotation = Quaternion.Euler(-3.46f, 0f, 0f);
            Vector3 scale = new Vector3(138.1644f, 138.1643f, 1381.641f);

            Main.Log("Instantiating stopwatch");

            try
            {

                var hudDash = transform.Find("Local/DashCanvas/HUDDash");

                // Instantiate Stopwatch prefab
                if (Main.Prefabs.TryGetValue("Stopwatch", out var stopwatchPrefab))
                {
                    var stopwatchObj = Instantiate(stopwatchPrefab, hudDash);
                    stopwatchObj.transform.localPosition = position;
                    stopwatchObj.transform.localRotation = rotation;
                    stopwatchObj.transform.localScale = scale;
                    stopwatchObj.SetActive(true);
                    
                    var stopwatch = stopwatchObj.GetComponent<Stopwatch>();
                    stopwatch.Battery = transform.GetComponentInChildren<Battery>();
                    stopwatch.HudDash = hudDash;
                }
                else
                {
                    Main.LogError("Could not find Stopwatch prefab!");
                    return;
                }

                // Set autopilot controls inactive
                hudDash.Find("Heading").gameObject.SetActive(false);
                hudDash.Find("Altitude").gameObject.SetActive(false);
                hudDash.Find("Speed").gameObject.SetActive(false);
                hudDash.Find("Nav").gameObject.SetActive(false);
                hudDash.Find("lrRectangle (2)").gameObject.SetActive(false);
                hudDash.Find("AutopilotLabel").gameObject.SetActive(false);

                // Can't just set APOff inactive because the PoweredAudioPoolSource for all the HUDDash
                // buttons is on APOff/offAPButton...
                var offAPButton = hudDash.Find("APOff/offAPButton");
                offAPButton.GetComponent<VRInteractable>().enabled = false;
                offAPButton.GetComponent<VRButton>().enabled = false;
                offAPButton.GetComponent<VTLocalizationComponentKey>().enabled = false;
                hudDash.Find("APOff/rectangleButtonBase (1)").gameObject.SetActive(false);
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not instantiate stopwatch!");
            }
        }
    }

    class Stopwatch : MonoBehaviour
    {
#pragma warning disable CS0649
        [Tooltip("Amount of charge used per second while the system is armed/pressurized.")]
        public float chargeDrainRatePerSecond;

        public Text displayText;
#pragma warning restore CS0649

        private bool isStopwatchRunning;
        private double elapsedTimeSeconds;

        private Transform hudDash;
        private AudioSource audioSource;
        private AudioClip clickSound;
        private PoseBounds hudDashPoseBounds;
        private Mesh rectangleButtonBaseMesh;
        private Mesh rectangleButtonMesh;
        private Mesh displayLabelMesh;
        private Material cockpitPropsMaterial;
        private Material textLabelMaterial;
        private VTTextFont textLabelFont;
        private Font veraMoBdFont;

        public Transform HudDash {
            get => hudDash;
            set
            {
                hudDash = value;
                GetAssetReferences();
                ConfigureStartStopButton();
                ConfigureResetButton();
                ConfigureDisplayAndLabel();
            }
        }

        public Battery Battery { get; set; }

        private void Update()
        {
            // Update time if stopwatch is running
            if (isStopwatchRunning)
            {
                elapsedTimeSeconds += Time.deltaTime;

                // Overflow after 100 minutes, the max value for the display
                if (elapsedTimeSeconds >= 100f * 60f)
                {
                    elapsedTimeSeconds -= 100f * 60f;
                }
            }

            // When the power is switched on, update the display
            // When the power is switched off, turn off the display, stop and reset the timer
            if (Battery != null && Battery.Drain(chargeDrainRatePerSecond * Time.deltaTime))
            {
                if (!displayText.enabled)
                {
                    displayText.enabled = true;
                }

                var timeSpan = TimeSpan.FromSeconds(elapsedTimeSeconds);

                displayText.text = timeSpan.ToString(@"mm\:ss\.f");
            }
            else
            {
                if (displayText.enabled)
                {
                    displayText.enabled = false;
                    isStopwatchRunning = false;
                    Reset();
                }
            }
        }

        public void StartStop()
        {
            isStopwatchRunning = !isStopwatchRunning;
        }

        public void Reset()
        {
            elapsedTimeSeconds = 0;
        }

        private void GetAssetReferences()
        {
            var uiAudio = transform.root.Find("UIAudio");
            var apOff = hudDash.Find("APOff");
            var offApButton = apOff.Find("offAPButton");
            var rectangleButtonBase = apOff.Find("rectangleButtonBase (1)");
            var rectangleButton = rectangleButtonBase.Find("rectangleButton");
            var displayLabel = hudDash.parent.Find("Dash/VerticalSpeedIndicator/Model (1)");
            var apOffLabel = rectangleButton.Find("apOffLabel");
            var flightLogText = hudDash.parent.Find("Dash/MFD1/MFDMask/MFDManager/FlightLog/Text");

            audioSource = uiAudio.GetComponent<AudioSource>();
            clickSound = offApButton.GetComponent<PoweredAudioPoolSource>().unpoweredSound;
            hudDashPoseBounds = offApButton.GetComponent<VRInteractable>().poseBounds;
            rectangleButtonBaseMesh = rectangleButtonBase.GetComponent<MeshFilter>().mesh;
            rectangleButtonMesh = rectangleButton.GetComponent<MeshFilter>().mesh;
            displayLabelMesh = displayLabel.GetComponent<MeshFilter>().mesh;
            cockpitPropsMaterial = rectangleButtonBase.GetComponent<MeshRenderer>().material;
            textLabelMaterial = apOffLabel.GetComponent<MeshRenderer>().material;
            textLabelFont = apOffLabel.GetComponent<VTText>().font;
            veraMoBdFont = flightLogText.GetComponent<Text>().font;
        }

        private void ConfigureStartStopButton()
        {
            try
            {
                var startStopButton = transform.Find("StartStop/startStopButton");

                var startStopInteractable = startStopButton.GetComponent<VRInteractable>();

                startStopInteractable.OnInteract.AddListener(() => audioSource.PlayOneShot(clickSound));
                startStopInteractable.poseBounds = hudDashPoseBounds;

                var startStopButtonBase = transform.Find("StartStop/rectangleButtonBase");

                startStopButtonBase.GetComponent<MeshFilter>().mesh = rectangleButtonBaseMesh;
                startStopButtonBase.GetComponent<MeshRenderer>().material = cockpitPropsMaterial;

                var startStopRectangleButton = startStopButtonBase.Find("rectangleButton");

                startStopRectangleButton.GetComponent<MeshFilter>().mesh = rectangleButtonMesh;
                startStopRectangleButton.GetComponent<MeshRenderer>().material = cockpitPropsMaterial;

                var startStopLabel = startStopRectangleButton.Find("startStopLabel");

                startStopLabel.GetComponent<MeshFilter>().mesh = new Mesh();
                startStopLabel.GetComponent<MeshRenderer>().material = textLabelMaterial;
                startStopLabel.GetComponent<VTText>().font = textLabelFont;
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not configure start/stop button!");
            }
        }

        private void ConfigureResetButton()
        {
            try
            {
                var resetButton = transform.Find("Reset/resetButton");

                var resetInteractable = resetButton.GetComponent<VRInteractable>();

                resetInteractable.OnInteract.AddListener(() => audioSource.PlayOneShot(clickSound));
                resetInteractable.poseBounds = hudDashPoseBounds;

                var resetButtonBase = transform.Find("Reset/rectangleButtonBase");

                resetButtonBase.GetComponent<MeshFilter>().mesh = rectangleButtonBaseMesh;
                resetButtonBase.GetComponent<MeshRenderer>().material = cockpitPropsMaterial;

                var resetRectangleButton = resetButtonBase.Find("rectangleButton");

                resetRectangleButton.GetComponent<MeshFilter>().mesh = rectangleButtonMesh;
                resetRectangleButton.GetComponent<MeshRenderer>().material = cockpitPropsMaterial;

                var resetLabel = resetRectangleButton.Find("resetLabel");

                resetLabel.GetComponent<MeshFilter>().mesh = new Mesh();
                resetLabel.GetComponent<MeshRenderer>().material = textLabelMaterial;
                resetLabel.GetComponent<VTText>().font = textLabelFont;
            }
            catch
            {
                Main.LogError("Could not configure reset button!");
            }
        }

        private void ConfigureDisplayAndLabel()
        {
            try
            {
                var display = transform.Find("Display");

                display.GetComponent<MeshFilter>().mesh = displayLabelMesh;
                display.GetComponent<MeshRenderer>().material = cockpitPropsMaterial;

                var displayText = display.Find("Image/StopwatchText");

                displayText.GetComponent<Text>().font = veraMoBdFont;

                var stopwatchLabel = transform.Find("StopwatchLabel");

                stopwatchLabel.GetComponent<MeshFilter>().mesh = new Mesh();
                stopwatchLabel.GetComponent<MeshRenderer>().material = textLabelMaterial;
                stopwatchLabel.GetComponent<VTText>().font = textLabelFont;
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not configure stopwatch display/label!");
            }
        }
    }
}
