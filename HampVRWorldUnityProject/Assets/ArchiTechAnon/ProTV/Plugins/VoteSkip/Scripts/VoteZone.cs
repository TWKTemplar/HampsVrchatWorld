
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class VoteZone : UdonSharpBehaviour
    {

        private VoteSkip skipTarget;
        private VRCPlayerApi local;
        private bool hasLocal = false;
        private bool hasSkip = false;
        private bool hasZones = false;
        private bool init = false;
        public bool debug = false;
        private string debugColor = "#ffaa66";
        private string debugLabel;

        private void _Initialize()
        {
            if (init) return;
            init = true;
            debugLabel = gameObject.name;
            local = Networking.LocalPlayer;
            hasLocal = VRC.SDKBase.Utilities.IsValid(local);
            var zones = GetComponents<Collider>();
            hasZones = zones != null && zones.Length > 0;
            if (hasZones) foreach (Collider z in zones) z.isTrigger = true;
        }

        void Start()
        {
            _Initialize();
        }

        public void _SetVoteSkip(VoteSkip skip)
        {
            skipTarget = skip;
            hasSkip = true;
        }

        new void OnPlayerTriggerEnter(VRCPlayerApi plyr)
        {
            if (hasSkip) { } else return;
            skipTarget.IN_PLAYER = plyr;
            skipTarget._Enter();
        }
        new void OnPlayerTriggerExit(VRCPlayerApi plyr)
        {
            if (hasSkip) { } else return;
            skipTarget.IN_PLAYER = plyr;
            skipTarget._Exit();
        }


        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(VoteZone)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(VoteZone)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(VoteZone)} ({debugLabel})</color>] {value}");
        }
    }
}
