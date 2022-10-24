using Steamworks;

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

            if (isMine)
            {
                smokeSystem.OnSetState += SmokeSystem_OnSetSmoke;
                Refresh();
                VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
            }
            else
            {
                hPEquipSmokeSystem.IsRemote = true;
                smokeSystem.IsRemote = true;
            }

            Main.Log("SmokeSystemSync.OnNetInitialized");
        }

        private void OnDestroy()
        {
            if (VTNetworkManager.hasInstance)
            {
                VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
            }
        }

        private void Instance_OnNewClientConnected(SteamId obj)
        {
            if (isMine)
            {
                Refresh(obj.Value);
            }
        }

        private void Refresh(ulong target = 0uL)
        {
            if (isMine)
            {
                if (target == 0uL)
                {
                    SendRPC("RPC_Smoke", smokeSystem.IsSmokeOn ? 1 : 0);
                }
                else
                {
                    SendDirectedRPC(target, "RPC_Smoke", smokeSystem.IsSmokeOn ? 1 : 0);
                }
            }
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
