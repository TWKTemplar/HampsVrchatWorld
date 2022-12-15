
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-9998)] // needs to initialize just after any TVManagerV2 script
    public class TVManagerV2ManualSync : UdonSharpBehaviour
    {

        private TVManagerV2 tv;

        [UdonSynced] private int state = 0;
        [UdonSynced] private VRCUrl urlMain = new VRCUrl("");
        [UdonSynced] private VRCUrl urlAlt = new VRCUrl("");
        [UdonSynced] private bool locked = false;
        [UdonSynced] private bool errorOrRetry = false;
        [UdonSynced] private int urlRevision = 0;
        [UdonSynced] private int videoPlayer = -1;
        [UdonSynced] private bool loading = false;
        [SerializeField] private string[] whitelistNames;
        private int[] hashList = new int[]{-824020220};
        private VRCPlayerApi local;
        private string debugLabel;
        private bool debug = true;
        private bool init = false;

        public void _Initialize() {
            if (init) return;
            init = true;
            local = Networking.LocalPlayer;
            if (whitelistNames == null) whitelistNames = new string[0];
            // TODO use a proper encryption lib for handling protection of the whiteList names
            if (whitelistNames.Length > 0)
            {
                hashList = new int[whitelistNames.Length];
                for (int i = 0; i < whitelistNames.Length; i++)
                {
                    string n = whitelistNames[i];
                    if (n != null && n.Length > 0)
                        hashList[i] = n.GetHashCode();
                }
            }
            whitelistNames = null; // remove the original name list for a bit of cheap security, though it's not much
            if (debugLabel == null) debugLabel = "TV not connected yet";
        }

        void Start() => _Initialize();

        new void OnPreSerialization()
        {
            // Extract data from TV for manual sync
            state = tv.stateSync;
            urlMain = tv.urlMainSync;
            urlAlt = tv.urlAltSync;
            // sanity check for rare case where unity serialization fucks up and nullifies the internal string of a supposedly empty VRCUrl
            if (urlMain.Get() == null) urlMain = VRCUrl.Empty;
            if (urlAlt.Get() == null) urlAlt = VRCUrl.Empty;
            locked = tv.lockedSync;
            urlRevision = tv.urlRevisionSync;
            loading = tv.loadingSync;
            videoPlayer = tv.videoPlayerSync;
            errorOrRetry = tv.errorOccurred || tv.retrying;
            log($"PreSerialization: ownerState {state} | locked {locked} | urlRevision {urlRevision} | videoPlayer {videoPlayer}");
            log($"Main URL {urlMain} | Alt URL {urlAlt} | Error/Retry {errorOrRetry}");
        }

        new void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        {
            if (result.success)
            {
                log("All good.");
            }
            else
            {
                warn("Failed to sync, retrying.");
                SendCustomEventDelayedSeconds(nameof(_RequestSync), 5f);
            }
        }

        new void OnDeserialization()
        {
            log($"Deserialization: ownerState {state} | locked {locked} | urlRevision {urlRevision} | videoPlayer {videoPlayer}");
            log($"Main URL {urlMain} | Alt URL {urlAlt}");
            // Update TV with new manually synced data
            tv.stateSync = state;
            tv.urlMainSync = urlMain;
            tv.urlAltSync = urlAlt;
            tv.lockedSync = locked;
            tv.urlRevisionSync = urlRevision;
            tv.loadingSync = loading;
            tv.videoPlayerSync = videoPlayer;
            tv.ownerErrorSync = errorOrRetry;
            tv._PostDeserialization();
        }

        public void _SetTV(TVManagerV2 tv)
        {
            this.tv = tv;
            debug = tv.debug;
            debugLabel = tv.gameObject.name;
            _Initialize();
        }

        public void _RequestSync()
        {
            log("Requesting manual serialization");
            RequestSerialization();
        }

        public bool _CheckWhitelist()
        {
            _Initialize();
            bool pass = Array.IndexOf(hashList, local.displayName.GetHashCode()) > -1;
            return pass;
        }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#cccc44>{nameof(TVManagerV2ManualSync)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#cccc44>{nameof(TVManagerV2ManualSync)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#cccc44>{nameof(TVManagerV2ManualSync)} ({debugLabel})</color>] {value}");
        }
    }
}
