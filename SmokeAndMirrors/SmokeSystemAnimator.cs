using UnityEngine;

namespace SmokeAndMirrors
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    class SmokeSystemAnimator : MonoBehaviour
    {
#pragma warning disable CS0649
        public AnimationCurve throttleCurve;
        public float abMultiplier;
        public string stateName;
        public int layer = -1;
#pragma warning restore CS0649

        private Animator animator;
        private int stateNameHash;
        private float lastPosition = -1f;

        public ModuleEngine Engine { get; set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            stateNameHash = Animator.StringToHash(stateName);
        }

        private void Update()
        {
            if (Engine != null)
            {
                float nozzlePosition = Engine.finalThrottle * (1f + Engine.abMult * abMultiplier);

                if (Mathf.Abs(nozzlePosition - lastPosition) > 0.001f)
                {
                    lastPosition = nozzlePosition;
                    animator.Play(stateNameHash, layer, throttleCurve.Evaluate(nozzlePosition));
                    animator.speed = 0f;
                }
            }
        }
    }
}
