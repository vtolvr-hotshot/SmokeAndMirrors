using UnityEngine;
using UnityEngine.Events;

namespace SmokeAndMirrors
{
    [RequireComponent(typeof(HPEquipSmokeSystem))]
    [DisallowMultipleComponent]
    class SmokeSystem : ElectronicComponent, IMassObject
    {
#pragma warning disable CS0649
        [Tooltip("Number of liters that a newly-equipped system should start with.")]
        public float startingOil;

        [Tooltip("Maximum capacity of the tank in liters.")]
        public float maxOil;

        [Tooltip("How long in minutes should a full tank last if used continuously?")]
        public float fullDurationMinutes;

        [Tooltip("Empty system mass in metric tons.")]
        public float systemMass;

        [Tooltip("Mass in metric tons of 1 liter of smoke oil.")]
        public float oilDensity;

        [Tooltip("Amount of charge in Baha Units™ used per second while the system is armed/pressurized.")]
        public float chargeDrainRatePerSecond;

        public SmokeSystemParticles smokeSystemParticles;
#pragma warning restore CS0649

        public event UnityAction<bool> OnSetState;
        public event UnityAction<bool> OnSetArmed;
        public event UnityAction<float, float> OnUpdateQuantity;

        private double currentOil;
        private bool isSmokeArmSwitchOn;
        private bool isSmokeToggleOn;

        public bool IsRemote { get; set; }
        public bool IsSmokeArmed { get; private set; }
        public bool IsSmokeOn { get; private set; }

        public float CurrentOil
        {
            get => (float)currentOil;
        }

        public float NormalizedOil
        {
            get => (float)currentOil / maxOil;
            set
            {
                currentOil = maxOil * Mathf.Clamp01(value);
            }
        }

        private void Awake()
        {
            currentOil = Mathf.Min(startingOil, maxOil);
        }

        private void Start()
        {
            if (OnUpdateQuantity != null) OnUpdateQuantity((float)currentOil, NormalizedOil);
        }

        private void Update()
        {
            if (IsRemote)
            {
                return;
            }

            // Set armed state
            if (isSmokeArmSwitchOn && battery != null && DrainElectricity(chargeDrainRatePerSecond * Time.deltaTime))
            {
                IsSmokeArmed = true;
                if (OnSetArmed != null) OnSetArmed(IsSmokeArmed);
            }
            else
            {
                IsSmokeArmed = false;
                isSmokeToggleOn = false;
                if (OnSetArmed != null) OnSetArmed(IsSmokeArmed);
            }

            // Set smoke on if system is armed, toggle is on, and oil tank is not empty
            if (!IsSmokeOn && IsSmokeArmed && isSmokeToggleOn && currentOil > 0d)
            {
                IsSmokeOn = true;
                if (OnSetState != null) OnSetState(IsSmokeOn);
            }

            // Set smoke off if system is not armed, toggle is off, or oil tank is empty
            if (IsSmokeOn && (!IsSmokeArmed || !isSmokeToggleOn || currentOil <= 0d))
            {
                IsSmokeOn = false;
                if (OnSetState != null) OnSetState(IsSmokeOn);
            }

            // Reduce current oil quantity if smoke is on
            if (IsSmokeOn && currentOil >= 0d)
            {
                currentOil -= Time.deltaTime / (fullDurationMinutes * 60d) * maxOil;
                if (OnUpdateQuantity != null) OnUpdateQuantity((float)currentOil, NormalizedOil);
            }

        }

        public void SetArmSwitchState(bool isSmokeArmSwitchOn)
        {
            this.isSmokeArmSwitchOn = isSmokeArmSwitchOn;
        }

        public void SetToggleOn()
        {
            isSmokeToggleOn = true;
        }

        public void SetToggleOff()
        {
            isSmokeToggleOn = false;
        }

        public void SetStateRemote(bool isSmokeOn)
        {
            if (IsRemote)
            {
                IsSmokeOn = isSmokeOn;
            }
        }

        public float GetMass()
        {
            return systemMass + (float)currentOil * oilDensity;
        }
    }
}
