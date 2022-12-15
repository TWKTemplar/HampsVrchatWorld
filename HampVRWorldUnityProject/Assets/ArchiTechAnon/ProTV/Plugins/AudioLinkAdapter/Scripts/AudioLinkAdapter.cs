
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioLinkAdapter : UdonSharpBehaviour
    {
#if !AUDIOLINK
        [Header("AudioLink is not present in the project. AudioLink is required for this plugin.")] 
        public string getAudioLinkHere = "https://github.com/llealloo/vrc-udon-audio-link/releases/latest";
        private bool debug = true;
        private string debugLabel;

        void Start() {
            debugLabel = $"<Missing AudioLink>/{name}";
            err("AudioLink is not present in the project. AudioLink is required for this plugin.");
            log("Get it here: https://github.com/llealloo/vrc-udon-audio-link/releases/latest");
        }
#else
        public TVManagerV2 tv;
        public VRCAudioLink.AudioLink audioLinkInstance;
        public string speakerName = "AudioLink";
        [Tooltip("Optionally specify world music to pause while the TV is playing. Will resume world music a given number of seconds after TV stops playing.")]
        public AudioSource worldMusic;
        [Tooltip("How long to wait after the TV has finished before resuming the world music.")]
        public float worldMusicResumeDelay = 20f;
        [Tooltip("How long does the world music take to fade in after the delay has completed.")]
        public float worldMusicFadeInTime = 4f;
        private float worldMusicVolume;
        private float worldMusicFadeAmount;
        private int OUT_VIDEOPLAYER;
        private bool worldMusicActive = true;
        private bool hasWorldMusic = false;
        private bool init = false;
        private bool debug = false;
        private string debugLabel;

        private void initialize()
        {
            if (init) return;
            hasWorldMusic = worldMusic != null;
            if (hasWorldMusic)
            {
                if (audioLinkInstance.audioSource != worldMusic)
                    audioLinkInstance.audioSource = worldMusic;
                worldMusicVolume = worldMusic.volume;
            }
            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            if (tv == null)
            {
                debugLabel = $"<Missing TV Ref>/{name}";
                err("The TV reference was not provided. Please make sure the audio link adapter knows what TV to connect to.");
                return;
            }
            debugLabel = $"{tv.gameObject.name}/{name}";
            tv._RegisterUdonSharpEventReceiver(this);
            init = true;
        }

        void Start()
        {
            initialize();
        }

        void Update() => _InternalUpdate();

        public void _InternalUpdate()
        {
            if (hasWorldMusic)
            {
                if (worldMusic.isPlaying && worldMusic.volume < worldMusicVolume)
                {
                    if (worldMusicFadeInTime == 0f)
                    {
                        worldMusic.volume = worldMusicVolume;
                    }
                    else
                    {
                        worldMusicFadeAmount += Time.deltaTime;
                        worldMusic.volume = Mathf.SmoothStep(0f, worldMusicVolume, worldMusicFadeAmount / worldMusicFadeInTime);
                    }
                }
            }
        }

        public void _TvMediaStart()
        {
            if (hasWorldMusic && worldMusicActive)
            {
                worldMusic.volume = 0f;
                worldMusicFadeAmount = 0f;
                worldMusic.Pause();
                worldMusicActive = false;
            }
            updateAudioSource(tv.videoPlayer);
        }

        public void _TvMediaEnd() => resumeWorldMusic();
        public void _TvStop() => resumeWorldMusic();

        private void resumeWorldMusic()
        {
            if (hasWorldMusic)
            {
                worldMusicActive = true;
                SendCustomEventDelayedSeconds(nameof(_ActivateWorldMusic), worldMusicResumeDelay);
            }
        }

        public void _ActivateWorldMusic()
        {
            if (hasWorldMusic && worldMusicActive)
            {
                log("Resuming world music...");
                audioLinkInstance.audioSource = worldMusic;
                worldMusic.UnPause();
            }
        }

        public void _TvVideoPlayerChange()
        {
            updateAudioSource(OUT_VIDEOPLAYER);
        }

        private void updateAudioSource(int videoPlayer)
        {
            var manager = tv.videoManagers[videoPlayer];
            log($"Updating AudioLink for {manager.gameObject.name}");
            var speakers = manager.speakers;
            foreach (var speaker in speakers)
            {
                if (speaker == null) continue;
                if (speaker.gameObject.name == speakerName)
                {
                    log("Valid source found");
                    audioLinkInstance.audioSource = speaker;
                    return;
                }
            }
            warn($"No audio source called {speakerName} was found connected to the {manager.gameObject.name} video manager.");
        }

#endif

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>{nameof(AudioLinkAdapter)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>{nameof(AudioLinkAdapter)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>{nameof(AudioLinkAdapter)} ({debugLabel})</color>] {value}");
        }
    }
}
