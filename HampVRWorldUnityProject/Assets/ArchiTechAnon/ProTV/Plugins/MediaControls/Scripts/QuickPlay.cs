
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class QuickPlay : UdonSharpBehaviour
    {
        public TVManagerV2 tv;
        public Queue queue;
        public VRCUrl url = new VRCUrl("");
        public VRCUrl altUrl = new VRCUrl("");
        public string title;
        private VRCPlayerApi local;
        private bool hasQueue;
        private bool init = false;
        private bool debug = false;
        private string debugLabel;
        private string debugColor = "#ffaa66";

        public void _Initialize()
        {
            if (init) return;
            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            if (tv == null) {
                debugLabel = $"<Missing TV Ref>/{name}";
                err("The TV reference was not provided. Please make sure the quickplay knows what TV to connect to.");
                return;
            }
            debugLabel = $"{tv.gameObject.name}/{name}";
            if (url == null) url = VRCUrl.Empty;
            if (altUrl == null) altUrl = VRCUrl.Empty;
            local = Networking.LocalPlayer;
            hasQueue = queue != null;
            debug = tv.debug;
            tv._RegisterUdonSharpEventReceiverWithPriority(this, 200);
            init = true;
        }

        void Start()
        {
            _Initialize();
        }

        new void Interact()
        {
            _Activate();
        }

        public void _Activate()
        {
            if (hasQueue){
                queue.IN_URL = url;
                queue.IN_ALT = altUrl;
                queue.IN_TITLE = title;
                queue._QueueMedia();
            } else tv._ChangeMediaWithAltTo(url, altUrl);
        }

        public void _TvMediaStart()
        {
            var tvUrl = tv.url.Get();
            if (tvUrl == url.Get() || tvUrl == altUrl.Get())
                tv.localLabel = title;
        }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(QuickPlay)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(QuickPlay)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(QuickPlay)} ({debugLabel})</color>] {value}");
        }
    }
}
