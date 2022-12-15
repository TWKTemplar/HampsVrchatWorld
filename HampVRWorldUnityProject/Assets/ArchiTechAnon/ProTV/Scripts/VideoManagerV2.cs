using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;

#if !COMPILER_UDONSHARP && UNITY_EDITOR


#endif

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(BaseVRCVideoPlayer))]
    [DefaultExecutionOrder(-9998)] // init immediately after the TV
    public class VideoManagerV2 : UdonSharpBehaviour
    {
        [NonSerialized] public BaseVRCVideoPlayer videoPlayer;
        [NonSerialized] public bool isVisible;
        [Tooltip("Flag whether or not to have this video manager should automatically control the managed speakers' audio setup (2d/3d).")]
        public bool autoManageAudioMode = true;
        [Tooltip("Flag whether or not to have this video manager should automatically control the managed speakers' volume.")]
        public bool autoManageVolume = true;
        [Tooltip("Flag whether or not to have this video manager should automatically control the managed speakers' mute state.")]
        public bool autoManageMute = true;
        [Tooltip("Amount to set the audio spread in degrees (0-360) when switching to 3D audio mode. Set to a negative number to disable updating the spread automatically.")]
        [Range(0f, 360f)] public float spread3D = -1f;
        [Tooltip("A custom name/label for the video manager that can be used by plugins. Typically shows up in any MediaControls dropdowns.")]
        public string customLabel = "";
        [Header("List of automatically-managed screens and speakers.")]
        [SerializeField] private GameObject[] managedScreens;
        [SerializeField] private AudioSource[] managedSpeakers;
        [Header("List of reference-only screens and speakers, helpful for plugins.")]
        [SerializeField] private GameObject[] unmanagedScreens;
        [SerializeField] private AudioSource[] unmanagedSpeakers;
        [HideInInspector] public GameObject[] screens; // combined list of managed and unmanaged
        [HideInInspector] public AudioSource[] speakers; // combined list of managed and unmanaged
        [NonSerialized] public Renderer[] renderers;
        private TVManagerV2 tv;
        private VideoError lastError;
        [NonSerialized] public bool muted = true;
        [NonSerialized] public float volume = 0.5f;
        [NonSerialized] public bool audio3d = true;

        // private TextureProxy texProxy;
        // private bool hasTexProxy;
        private bool init = false;
        private bool debug = false;
        private string debugLabel;

        private void initialize()
        {
            if (init) return;
            videoPlayer = (BaseVRCVideoPlayer)GetComponent(typeof(BaseVRCVideoPlayer));
            videoPlayer.EnableAutomaticResync = false;
            // texProxy = GetComponentInChildren<TextureProxy>(true);
            // hasTexProxy = texProxy != null;
            debugLabel = name;
            // 2.1 upgrade handling for the new field names. 
            // screens/speakers fields used to contain the whole list of the respective types, 
            // but are to now be used as a composite list of managed and unmanaged that is exposed publicly within the compiler.
            // Using managedScreens as the target for backwards compatible behaviour with how it was before.
            if (screens == null) screens = new GameObject[0];
            if (speakers == null) speakers = new AudioSource[0];
            if (managedScreens == null || managedScreens.Length == 0)
                if (unmanagedScreens == null || unmanagedScreens.Length == 0)
                    managedScreens = screens;
            if (managedSpeakers == null || managedSpeakers.Length == 0)
                if (unmanagedSpeakers == null || unmanagedSpeakers.Length == 0)
                    managedSpeakers = speakers;
            if (unmanagedScreens == null) unmanagedScreens = new GameObject[0];
            if (unmanagedSpeakers == null) unmanagedSpeakers = new AudioSource[0];

            // combine screen list internally
            screens = new GameObject[managedScreens.Length + unmanagedScreens.Length];
            Array.Copy(managedScreens, 0, screens, 0, managedScreens.Length);
            Array.Copy(unmanagedScreens, 0, screens, managedScreens.Length, unmanagedScreens.Length);

            renderers = new Renderer[screens.Length];
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == null) continue;
                renderers[i] = screens[i].GetComponent<Renderer>();
            }

            // combine speaker list internally
            speakers = new AudioSource[managedSpeakers.Length + unmanagedSpeakers.Length];
            Array.Copy(managedSpeakers, 0, speakers, 0, managedSpeakers.Length);
            Array.Copy(unmanagedSpeakers, 0, speakers, managedSpeakers.Length, unmanagedSpeakers.Length);
            init = true;
        }
        void Start()
        {
            initialize();
            log($"Hiding self");
            _Hide();
        }


        // === Player Proxy Methods ===

        // new void OnVideoStart() => tv._OnVideoPlayerStart();
        new void OnVideoEnd() => tv._OnVideoPlayerEnd();
        new void OnVideoError(VideoError error) => tv._OnVideoPlayerError(error);
        // new void OnVideoLoop() => tv.OnVideoPlayerLoop();
        // new void OnVideoPause() => tv.OnVideoPlayerPause();
        // new void OnVideoPlay() => tv.OnVideoPlayerPlay();
        new void OnVideoReady() => tv._OnVideoPlayerReady();


        // === Public events to control the video player parts ===

        public void _Show()
        {
            if (!init) initialize();
            foreach (var screen in managedScreens)
            {
                if (screen == null) continue;
                screen.SetActive(true);
            }
            if (autoManageMute) _UnMute();
            isVisible = true;
            // if (hasTexProxy) texProxy.enabled = true;
            if (tv != null)
                log($"{tv.gameObject.name} [{gameObject.name}] activated");
        }

        public void _Hide()
        {
            if (!init) initialize();
            if (autoManageMute) _Mute();
            foreach (var screen in managedScreens)
            {
                if (screen == null) continue;
                screen.SetActive(false);
            }
            isVisible = false;
            // if (hasTexProxy) texProxy.enabled = false;
        }

        public void _Stop()
        {
            _Hide();
            videoPlayer.Stop();
            log("Deactivated");
        }

        public void _ApplyStateTo(VideoManagerV2 other)
        {
            if (autoManageVolume)
            {
                other._ChangeMute(muted);
                other._ChangeVolume(volume);
            }
            if (autoManageAudioMode)
            {
                other._ChangeAudioMode(audio3d);
            }
        }

        public void _ChangeMute(bool muted)
        {
            if (!init) initialize();
            this.muted = muted;
            foreach (AudioSource speaker in managedSpeakers)
            {
                if (speaker == null) continue;
                log($"Setting {speaker.gameObject.name} Mute to {muted}");
                speaker.mute = muted;
            }
        }
        public void _Mute() => _ChangeMute(true);
        public void _UnMute() => _ChangeMute(false);


        public void _ChangeAudioMode(bool use3dAudio)
        {
            if (!init) initialize();
            this.audio3d = use3dAudio;
            float blend = use3dAudio ? 1.0f : 0.0f;
            float spread = use3dAudio ? spread3D : 360f;
            foreach (AudioSource speaker in managedSpeakers)
            {
                if (speaker == null) continue;
                speaker.spatialize = use3dAudio;
                speaker.spatialBlend = blend;
                if (spread3D >= 0) speaker.spread = spread;
            }
        }
        public void _Use3dAudio() => _ChangeAudioMode(true);
        public void _Use2dAudio() => _ChangeAudioMode(false);


        public void _ChangeVolume(float volume)
        {
            if (!init) initialize();
            this.volume = volume;
            foreach (AudioSource speaker in managedSpeakers)
            {
                if (speaker == null) continue;
                speaker.volume = volume;
            }
        }


        // ================= Helper Methods =================

        public void _SetTV(TVManagerV2 manager)
        {
            tv = manager;
            debug = tv.debug;
            debugLabel = $"{tv.gameObject.name}/{name}";
            // if (hasTexProxy) texProxy.debug = debug;
        }

        // Disabled for now as it doesn't work correctly
        // IMPORTANT NOTE: This can be a very expensive operation depending on the original setup.
        // public Texture2D _GetVideoTexture()
        // {
        //     if (hasTexProxy) return texProxy._GetVideoTexture();
        //     warn("No Texture Proxy assigned to this video manager. Unable to get the video texture.");
        //     return null;
        // }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ccaa>{nameof(VideoManagerV2)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> |  <color=#00ccaa>{nameof(VideoManagerV2)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> |  <color=#00ccaa>{nameof(VideoManagerV2)} ({debugLabel})</color>] {value}");
        }
    }
}