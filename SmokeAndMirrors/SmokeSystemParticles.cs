using System;

using UnityEngine;

namespace SmokeAndMirrors
{
    [DisallowMultipleComponent]
    class SmokeSystemParticles : MonoBehaviour
    {
        private static Material smokeMaterial;

#pragma warning disable CS0649
        public SmokeSystem smokeSystem;

        [Header("Short Lasting, High Density Particles")]
        public ParticleSystem smokeParticlesShort;

        [Tooltip("Number of particles to emit per second per knot of airspeed.")]
        public float particlesPerKnotShort;

        [Tooltip("Minimum number of particles to emit per second.")]
        public float minRateShort;

        [Tooltip("Maximum number of particles to emit per second.")]
        public float maxRateShort;

        [Header("Long Lasting, Low Density Particles")]
        public ParticleSystem smokeParticlesLong;

        [Tooltip("Number of particles to emit per second per knot of airspeed.")]
        public float particlesPerKnotLong;

        [Tooltip("Minimum number of particles to emit per second.")]
        public float minRateLong;

        [Tooltip("Maximum number of particles to emit per second.")]
        public float maxRateLong;
#pragma warning restore CS0649

        public ModuleEngine Engine { get; set; }
        public FlightInfo FlightInfo { get; set; }

        private bool areParticleSystemsPlaying;
        private ParticleSystemRenderer shortRenderer;
        private ParticleSystemRenderer longRenderer;

        private void Awake()
        {
            shortRenderer = smokeParticlesShort.GetComponent<ParticleSystemRenderer>();
            longRenderer = smokeParticlesLong.GetComponent<ParticleSystemRenderer>();
            UpdateMaterial();
        }

        private void Update()
        {
            if (Engine == null)
            {
                return;
            }

            // Align the particle system transforms with the engine thrust transform
            smokeParticlesShort.transform.rotation = Engine.thrustTransform.rotation;
            smokeParticlesLong.transform.rotation = Engine.thrustTransform.rotation;

            // Start the particle systems when the smoke system and engine are on, and the engine is not in afterburner
            if (!areParticleSystemsPlaying && smokeSystem.IsSmokeOn && Engine.startedUp && !Engine.afterburner)
            {
                Play();
            }

            // Stop the particle systems when the smoke system or engine are off, or the engine is in afterburner
            if (areParticleSystemsPlaying && (!smokeSystem.IsSmokeOn || !Engine.startedUp || Engine.afterburner))
            {
                Stop();
            }
        }

        private void FixedUpdate()
        {
            // Update particle emission rate based on airspeed
            if (FlightInfo != null)
            {
                float knots = MeasurementManager.SpeedToKnot(FlightInfo.airspeed);

                float emissionRateShort = Mathf.Clamp(knots * particlesPerKnotShort, minRateShort, maxRateShort);
                var emissionShort = smokeParticlesShort.emission;
                emissionShort.rateOverTime = emissionRateShort;

                float emissionRateLong = Mathf.Clamp(knots * particlesPerKnotLong, minRateLong, maxRateLong);
                var emissionLong = smokeParticlesLong.emission;
                emissionLong.rateOverTime = emissionRateLong;
            }
        }

        private void Play()
        {
            smokeParticlesShort.Play();
            smokeParticlesLong.Play();
            areParticleSystemsPlaying = true;
        }

        private void Stop()
        {
            smokeParticlesShort.Stop();
            smokeParticlesLong.Stop();
            areParticleSystemsPlaying = false;
        }

        private void UpdateMaterial()
        {
            if (smokeMaterial == null)
            {
                Main.Log("Loading AIM-120 smoke trail material");

                var aim120 = VTResources.Load<GameObject>("weapons/missiles/AIM-120");

                try
                {
                    var missileTrail = aim120.transform.Find("exhaustTransform/MissileTrail");
                    var missileParticleSystemRenderer = missileTrail.GetComponent<ParticleSystemRenderer>();
                    smokeMaterial = new Material(missileParticleSystemRenderer.material);
                }
                catch (NullReferenceException)
                {
                    Main.LogError("Could not load AIM-120 particle material!");
                }
            }

            shortRenderer.material = smokeMaterial;
            longRenderer.material = smokeMaterial;
        }
    }
}
