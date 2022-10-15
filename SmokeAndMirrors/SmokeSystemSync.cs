using UnityEngine;

using VTNetworking;

namespace SmokeAndMirrors
{
    [RequireComponent(typeof(HPEquipSmokeSystem))]
    [RequireComponent(typeof(SmokeSystem))]
    [DisallowMultipleComponent]
    class SmokeSystemSync : VTNetSyncRPCOnly
    {
        private HPEquipSmokeSystem hPEquipSmokeSystem;
        private SmokeSystem smokeSystem;

        protected override void Awake()
        {
            base.Awake();
            hPEquipSmokeSystem = GetComponent<HPEquipSmokeSystem>();
            smokeSystem = GetComponent<SmokeSystem>();
        }

        protected override void OnNetInitialized()
        {
            base.OnNetInitialized();

            if (isMine && smokeSystem != null)
            {
                smokeSystem.OnSetState += SmokeSystem_OnSetSmoke;
            }

            if (!isMine)
            {
                hPEquipSmokeSystem.IsRemote = true;
                smokeSystem.IsRemote = true;
            }

            Main.Log("SmokeSystemSync.OnNetInitialized");
        }

        private void SmokeSystem_OnSetSmoke(bool isSmokeOn)
        {
            SendRPC("RPC_Smoke", isSmokeOn ? 1 : 0);
        }

        [VTRPC]
        private void RPC_Smoke(int state)
        {
            Main.Log($"RPC_Smoke({state}) from {netEntity?.owner.Name}");
            smokeSystem.SetStateRemote(state > 0);
        }
    }
}
