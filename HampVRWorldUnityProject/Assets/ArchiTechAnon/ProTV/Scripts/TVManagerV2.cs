
using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(-9999)] // needs to initialize before anything else if possible
    public class TVManagerV2 : UdonSharpBehaviour
    {
        // used for special cases where the jumptime should be dynamically calculated
        private readonly float EPSILON = Mathf.Epsilon;

        // list of all events that the TVManagerV2 produces.
        private const string EVENT_READY = "_TvReady";
        private const string EVENT_PLAY = "_TvPlay";
        private const string EVENT_PAUSE = "_TvPause";
        private const string EVENT_STOP = "_TvStop";
        private const string EVENT_MEDIASTART = "_TvMediaStart";
        private const string EVENT_MEDIAEND = "_TvMediaEnd";
        private const string EVENT_MEDIALOOP = "_TvMediaLoop";
        private const string EVENT_MEDIACHANGE = "_TvMediaChange";
        private const string EVENT_OWNERCHANGE = "_TvOwnerChange";
        private const string EVENT_VIDEOPLAYERCHANGE = "_TvVideoPlayerChange";
        private const string EVENT_VIDEOPLAYERERROR = "_TvVideoPlayerError";
        private const string EVENT_MUTE = "_TvMute";
        private const string EVENT_UNMUTE = "_TvUnMute";
        private const string EVENT_VOLUMECHANGE = "_TvVolumeChange";
        private const string EVENT_AUDIOMODE3D = "_TvAudioMode3d";
        private const string EVENT_AUDIOMODE2D = "_TvAudioMode2d";
        private const string EVENT_ENABLELOOP = "_TvEnableLoop";
        private const string EVENT_DISABLELOOP = "_TvDisableLoop";
        private const string EVENT_SYNC = "_TvSync";
        private const string EVENT_DESYNC = "_TvDeSync";
        private const string EVENT_LOCK = "_TvLock";
        private const string EVENT_UNLOCK = "_TvUnLock";
        private const string EVENT_LOADING = "_TvLoading";
        private const string EVENT_LOADINGEND = "_TvLoadingEnd";
        private const string EVENT_LOADINGSTOP = "_TvLoadingAbort";

        // These variable names are used to pass information back to any event listeners that have been registered.
        // They follow the pattern of the word OUT, and a meaningful name on what that variable is related to.

        // Sets value as a VideoError type
        private const string OUT_ERROR = "OUT_ERROR";
        // Sets value as a float type
        private const string OUT_VOLUME = "OUT_VOLUME";
        // Sets value as an int type
        private const string OUT_VIDEOPLAYER = "OUT_VIDEOPLAYER";
        // Sets value as an int type
        private const string OUT_OWNER = "OUT_OWNER";

        private readonly char[] SPLIT_QUERY = new char[] { '?' };
        private readonly char[] SPLIT_QUERY_PARAM = new char[] { '&' };
        private readonly char[] SPLIT_HASH = new char[] { '#' };
        private readonly char[] SPLIT_HASH_PARAM = new char[] { ';' };
        private readonly char[] SPLIT_VALUE = new char[] { '=' };


        // === Event input variables (update these from external udon graphs. U# should use the corresponding parameterized methods instead) ===
        // These fields represent storage for incoming data from other scripts. 
        // The format is as follows: the word IN, the event that the data is related to, 
        //  the expected type of the data, and a meaningful name on what that variable is related to.
        // Eg: IN_VOLUME means the data is incoming, will be used by the event named '_ChangeVolume', 
        //  and represents the TV's volume percent value as a normalized float (between 0.0 and 1.0).

        // parameter for _ChangeVideo event
        [NonSerialized] public VRCUrl IN_URL = VRCUrl.Empty;
        [NonSerialized] public VRCUrl IN_ALT = VRCUrl.Empty;
        // parameter for _ChangeVolume event, expects it to be a normalized float between 0f and 1f
        [NonSerialized] public float IN_VOLUME = 0f;
        // parameter for _ChangeSeekTime event
        // parameter for the _ChangeSeekPercent event, expects it to be a normalized float between 0f and 1f
        [NonSerialized] public float IN_SEEK = 0f;
        // paramter for _ChangeVideoPlayer event
        [NonSerialized] public int IN_VIDEOPLAYER = -1;
        [NonSerialized] public UdonBehaviour IN_SUBSCRIBER;
        [NonSerialized] public byte IN_PRIORITY = 128;
        private const byte defaultPriority = 128;


        // enum values for state/stateSync/currentState
        private const int STOPPED = 0;
        private const int PLAYING = 1;
        private const int PAUSED = 2;


        public VideoManagerV2[] videoManagers;

        [Header("Autoplay Settings")]

        [Tooltip("This is the URL to set as automatically playing when the first user joins a new instance. This has no bearing on an existing instance as the TV has already been syncing data after the initial point.")]
        public VRCUrl autoplayURL = new VRCUrl("");

        [Tooltip("This is an optional alternate url that can be provided for situations when the main url is insufficient (such as an alternate stream endpoint for Quest to use)")]
        public VRCUrl autoplayURLAlt = new VRCUrl("");

        [Tooltip("Optional string to use as the label for the autoplay urls. Generally replaces the domain name in the UIs.")]
        public string autoplayLabel = string.Empty;


        [Header("Default TV Settings")]

        [Tooltip("The volume that the TV starts off at.")]
        [Range(0f, 1f)] public float initialVolume = 0.3f;

        [Tooltip("The player (based on the VideoManagers list below) for the TV to start off on.")]
        public int initialPlayer = 0;

        [Tooltip("Flag to initialize the TV with 2D audio instead of 3D audio")]
        public bool startWith2DAudio = false;


        [Header("Sync Options")]
        // This flag is to track whether or not the local player is able to operate independently of the owner
        // Setting to false gives the local player full control of their local player. 
        // Once they value is set to true, it will automatically resync with the owner, even if the video URL has changed since desyncing.
        [Tooltip("Flag that determines whether the video player should sync with the owner. If false, the local user has full control over the player and only affects the local user.")]
        public bool syncToOwner = true;

        [Min(5f), Tooltip("The interval for the TV to trigger an automatic resync to correct any AV and Time desync issues. Defaults to 5 minutes.")]
        public float automaticResyncInterval = 300f;

        [Tooltip("Time difference allowed between owner's synced seek time and the local seek time while the video is paused locally. Can be thought of as a 'frame preview' of what's currently playing. It's good to have this at a higher value, NOT recommended to have this value less than 1.0")]
        public float pausedResyncThreshold = 5.0f;

        [Tooltip("Flag that determines whether the current video player selection will be synced across users.")]
        public bool syncVideoPlayerSelection = false;


        [Header("Media Load Options")]

        [Tooltip("Flag to specify if the media should play immediately after it's been loaded. Unchecked means the media must be manually played to start.")]
        public bool playVideoAfterLoad = true;

        [Tooltip("Amount of time (in seconds) to wait before playing the media after it's successfully been loaded.")]
        public float bufferDelayAfterLoad = 0f;

        [Range(5f, 60f), Tooltip("The amount of time allowed for any given media to attempt loading. If the timeout is exceeded, it will fail with a VideoError.PlayerError")]
        public float maxAllowedLoadingTime = 20f;


        [Header("Security Options")]

        // this option is explicitly used for the edge case where world owners want to have anyone use the TV regardless.
        // It prevents the instance master from being able to lock the TV down by any means, when the world creator doesn't want them to.
        // Helps against malicious users in the edge case. 
        [Tooltip("This option enables the instance master to have admin powers for the TV. Leaving enabled should be perfectly acceptable in most cases.")]
        public bool allowMasterControl = true;
        [Tooltip("Determines if the video player starts off as locked down to master only. Good for worlds that do public events and similar.")]
        public bool lockedByDefault = false;


        [Header("Error/Retry Options")]

        [Min(0), Tooltip("The number of times a url should retry if no explicit retry amount is specified for a given url.")]
        public int defaultRetryCount = 0;

        [Min(5f), Tooltip("Amount of time (in seconds) to wait before reloading the media after an error occurs if the url specifies infinite retries.")]
        public float repeatingRetryDelay = 15f;

        [Tooltip("When attempting to retry a url, it will swap to the alternate url and try it instead. If that also fails, it will simply resume any remaining retries with the main url.")]
        public bool retryUsingAlternateUrl = true;


        [Header("Misc Options")]

        [Tooltip("Set this flag to have the TV auto-hide the initial video player after initialization. This is useful for preventing the video player from auto-showing itself when an autoplay video starts.")]
        public bool startHidden = false;

        [Tooltip("Set this flag to have the TV auto-disable itself after initialization.")]
        public bool startDisabled = false;

        [Tooltip("Whether or not to allow media to be loaded while the TV is in the hidden state.")]
        public bool stopMediaWhenHidden = false;

        [Tooltip("Determine whether or not to stop or simply pause any active media when the TV's game object is disabled.")]
        public bool stopMediaWhenDisabled = false;

        // This is auto-populated during the build phase
        [HideInInspector] public float autoplayStartOffset = 0f;

        // public TextAsset versionFile;

        // [SerializeField, HideInInspector] 
        // private string version = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ArchiTechAnon/ProTV/VERSION.md").text.Trim();

        [Space]

        // Storage for all event subscriber behaviors. 
        // Due to some odd type issues, it requires being stored as a list of components instead of UdonBehaviors.
        // All possible events that this TV triggers will be sent to ALL targets in order of addition
        private Component[] eventTargets;
        private Component[] skipTargets;
        private byte[] eventTargetWeights;

        // === Video Manager control ===
        // assigned when the active manager switches to the next one.
        private VideoManagerV2 prevManager;
        // main manager reference that everything operates off of.
        [NonSerialized] public VideoManagerV2 activeManager;
        // assigned when the user selects a manager to switch to.
        private VideoManagerV2 nextManager;

        // === Synchronized variables and their local counterparts ===

        private TVManagerV2ManualSync syncData;

        [NonSerialized][UdonSynced] public float syncTime = 0f;
        [NonSerialized] public float currentTime;
        [UdonSynced] private int lagCompSync;
        private float lagComp;
        // ownerState/localState values: 0 = stopped, 1 = playing, 2 = paused
        // ownerState is the value that is synced
        // localState is the sync tracking counterpart (used to detect state change from the owner)
        // currentState is the ACTUAL state that the local video player is in.
        // localState and currentState are separated to allow for local to not be forced into the owner's state completely
        // The primary reason for this deleniation is to allow for the local to pause without having to desync.
        // For eg: Someone isn't interested in most videos, but still wants to know what is playing, so they pause it and let it do the pausedThreshold resync (every 5 seconds)
        //      One could simply mute the video, yes, but some people might not want the distraction of an active video playing if they happen to be in front of a mirror
        //      where the TV is reflected. This allows a much more comfortable "keep track of" mode for those users.
        [NonSerialized] public int stateSync = STOPPED;
        // state of the owner user
        [NonSerialized] public int state = STOPPED;
        // state of the local user
        [NonSerialized] public int currentState = STOPPED;
        [NonSerialized] public VRCUrl url = VRCUrl.Empty;
        [NonSerialized] public VRCUrl urlMain = VRCUrl.Empty;
        [NonSerialized] public VRCUrl urlMainSync = VRCUrl.Empty;
        [NonSerialized] public VRCUrl urlAlt = VRCUrl.Empty;
        [NonSerialized] public VRCUrl urlAltSync = VRCUrl.Empty;
        [NonSerialized] public bool useAlternateUrl = false;
        // a miscellaneous string that is used to describe the current video. 
        // Allows for different extensions to share things like custom video titles in a centralized way.
        // Is automatically by default the current URL's full domain name.
        // The ideal place to update this is during the _TvMediaStart event.
        [NonSerialized] public string localLabel = EMPTY;
        [NonSerialized] public string[] urlHashParams = new string[0];
        [NonSerialized] public string[] urlHashValues = new string[0];
        [NonSerialized] public bool lockedSync = false;
        [NonSerialized] public bool locked = false;
        [NonSerialized] public int urlRevisionSync;
        [NonSerialized] public int urlRevision;
        [NonSerialized] public bool loadingSync;
        [NonSerialized] public bool loading;
        [NonSerialized] public bool ownerErrorSync = false;
        [NonSerialized] public bool ownerError = false;


        // === Fields for tracking internal state ===
        [NonSerialized] public float startTime;
        [NonSerialized] public float endTime;
        // actual length of the loaded media
        [NonSerialized] public float mediaLength;
        // amount of the time that the video is set to play for (basically endTime minus startTime)
        [NonSerialized] public float videoDuration;
        [NonSerialized] public int loop;
        [NonSerialized] public int retryCount = 0;
        [NonSerialized] public bool mute;
        [NonSerialized] public bool audio3d = true;
        [NonSerialized] public float volume = 0.5f;
        [NonSerialized] public int videoPlayerSync = -1;
        [NonSerialized] public int videoPlayer = -1;
        [NonSerialized] public bool isLive = false;
        [NonSerialized] public bool videoLoaded = false;
        [NonSerialized] public bool manualLoop = false;
        [NonSerialized] public bool mediaEnded = true;

        // Time delay before allowing the TV to update it's active video
        // This value is always assigned as: Time.realtimeSinceStartup + someOffsetValue;
        // It is checked using this structure: if (Time.realtimeSinceStartup < waitUntil) { waitIsOver(); }
        private float waitUntil = 0f;
        // Time to seek to at time sync check
        // This value is set for a couple different reasons.
        // If the video player is switching locally to a different player, it will use Mathf.Epsilon to signal seemless seek time for the player being swapped to.
        // If the video URL contains a t=, startat=, starttime=, or start= hash params, it will assign that value so to start the video at that point once it's loaded.
        private float jumpToTime = 0f;
        // This flag simply enables the local player to be paused without forcing hard-sync to the owner's state.
        // This results in a pause that, when the owner pauses then plays, it won't foroce the local player to unpause unintentionally.
        // This flag cooperates with the pausedThreshold constant to enable resyncing every 5 seconds without actually having the video playing.
        private bool locallyPaused = false;
        // Flag to check if an error occured. When true, it will prevent auto-reloading of the video.
        // Player will need to be forced to refresh by intentionally pressing (calling the method) Play.
        // The exception to this rule is when the error is of RateLimited type. 
        // This error will trigger a auto-reload after a 3 second delay to prevent excess requests from spamming the network causing more rate limiting triggers.
        [NonSerialized] public bool errorOccurred = false;


        // === Flags used to prevent infinite event loops ==
        // (like volume change -> _ChangeVolume -> _TvVolumeChange -> volume change -> _ChangeVolume -> _TvVolumeChange -> etc)
        private string[] haltedEvents = new string[23];

        // === Misc variables ===
        // private MaterialPropertyBlock matPropBlock;
        // private string playerNameOverride = EMPTY;
        [NonSerialized] public bool refreshAfterWait = false;
        private bool sendEvents = false;
        private bool activeInitialized = false;
        private bool enforceSyncTime = true;
        private float syncEnforceWait;
        private float loadingWait;
        private float autoSyncWait;
        private bool manuallyHidden = false;
        private bool buffering = false;
        private float syncEnforcementTimeLimit = 3f;
        private float reloadStart = -1f;
        private float reloadCache = -1f;
        private int mediaHash;
        private float nextUrlAttemptAllowed;
        [NonSerialized] public bool retrying = false;
        private bool retryingWithAlt = false;
        private bool retriedWithAlt = false;
        // private bool postInit = false;
        private bool isOwner { get => Networking.IsOwner(gameObject); }
        private bool lockedByInstanceOwner = false;
        private bool hasActiveManager = false;
        private bool hasLocalPlayer = false;
        private bool hasSyncData = false;
        private VRCPlayerApi localPlayer;
#if UNITY_ANDROID
        private bool isQuest = true;
#else
        private bool isQuest = false;
#endif
        private bool interactionState = true;
        public bool debug = true;
        [NonSerialized] public bool init = false;
        [NonSerialized] public bool tvInit = false;
        private const string EMPTY = "";

        private void initialize()
        {
            if (init) return;
            init = true;
            log($"Starting TVManagerV2");
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = VRC.SDKBase.Utilities.IsValid(localPlayer);
            cacheSyncData();
            if (videoManagers == null) videoManagers = GetComponentsInChildren<VideoManagerV2>(true);
            if (repeatingRetryDelay < 5f) repeatingRetryDelay = 5f;
            if (videoManagers != null)
            {
                bool noManagers = true;
                foreach (VideoManagerV2 m in videoManagers)
                {
                    if (m != null)
                    {
                        m._SetTV(this);
                        noManagers = false;
                    }
                }
                if (noManagers)
                {
                    err("No video managers available. Make sure any desired video managers are properly associated with the TV, otherwise the TV will not work.");
                    return;
                }
            }
            if (!syncToOwner || isOwner)
            {
                if (autoplayURL == null) autoplayURL = VRCUrl.Empty;
                if (autoplayURLAlt == null) autoplayURLAlt = VRCUrl.Empty;
                IN_URL = urlMain = urlMainSync = autoplayURL;
                IN_ALT = urlAlt = urlAltSync = autoplayURLAlt;
                localLabel = autoplayLabel;
            }
            // determine initial locked state
            if (lockedByDefault)
            {
                if (isOwner) lockedSync = true;
                locked = true;
            }

            useAlternateUrl = isQuest;
            // load initial video player
            videoPlayer = initialPlayer;
            nextManager = videoManagers[videoPlayer];
            if (isOwner) videoPlayerSync = videoPlayer;
            volume = initialVolume;
            nextManager._ChangeVolume(volume);
            if (startWith2DAudio) _AudioMode2d();
            else _AudioMode3d();
            if (startHidden) manuallyHidden = true;
            if (startDisabled) gameObject.SetActive(false);
            if (isOwner) syncData._RequestSync();
            // make the script wait a few seconds before trying to fetch the video data for the first time.
            waitUntil = Time.realtimeSinceStartup + 5f + autoplayStartOffset;
            // implicitly add 0.5 seconds to the buffer delay to guarentee that 
            // the syncTime continuous sync will be able to transmit prior to playing the media
            // This prevents non-owners from trying to sync to the wrong part of the media
            if (syncToOwner && bufferDelayAfterLoad < 0.5f) bufferDelayAfterLoad = 0.5f;
            if (automaticResyncInterval == 0f) automaticResyncInterval = Mathf.Infinity;
            autoSyncWait = Time.realtimeSinceStartup + automaticResyncInterval;
            syncEnforceWait = waitUntil + syncEnforcementTimeLimit;
        }

        private void cacheSyncData()
        {
            if (hasSyncData) return;
            else
            {
                syncData = transform.GetComponentInChildren<TVManagerV2ManualSync>(true);
                hasSyncData = syncData != null;
                if (hasSyncData) { }
                else err("MISSING TVManagerV2ManualSync component. Please ensure there is a child object with this component attached. The TV will not work properly without it.");
                syncData._SetTV(this);
            }
        }

        // === Subscription Methods ===

        void Start()
        {
            log("Start");
            initialize();
        }

        void OnEnable()
        {
            log("Enable");
            initialize();
            // if the TV was disabled before Update check ran, the TV was set off by default from some external method, like a Touch Control or likewise.
            // If that's the case, the startDisabled flag is redundant so simply unset the flag.
            if (startDisabled) startDisabled = false;
            if (hasActiveManager)
            {
                if (isOwner)
                {
                    if (reloadCache > 0f)
                    {
                        var diff = Time.realtimeSinceStartup - reloadStart;
                        jumpToTime = reloadCache + diff;
                        if (jumpToTime > endTime) jumpToTime = endTime;
                        if (!stopMediaWhenDisabled)
                            activeManager.videoPlayer.SetTime(jumpToTime);
                    }
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerEnabled));
                }
                _Play();
            }
        }

        void OnDisable()
        {
            if (hasActiveManager)
            {
                // In order to prevent a loop glitch due to owner not updating syncTime when the object is disabled
                // send a command as owner to everyone to pause the video. 
                // There are other solutions that might work, but this is the most elegant that could be found so far.
                if (isOwner)
                {
                    if (!isLive)
                    {
                        reloadCache = activeManager.videoPlayer.GetTime();
                        reloadStart = Time.realtimeSinceStartup;
                    }
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerDisabled));
                }
                if (stopMediaWhenDisabled) _Stop();
                else _Pause();
            }
        }

        void Update() => _InternalUpdate();

        public void _InternalUpdate()
        {
            // has not yet been initialized or is playmode without cyanemu
            if (init) if (hasLocalPlayer) { } else return; else return;
            var time = Time.realtimeSinceStartup;
            // wait until the timeout has cleard
            if (time < waitUntil) return;
            if (activeManager != nextManager)
            {
                if (debug)
                {
                    string nMan = nextManager == null ? "null" : nextManager.gameObject.name;
                    string aMan = activeManager == null ? "null" : activeManager.gameObject.name;
                    string pMan = prevManager == null ? "null" : prevManager.gameObject.name;
                    log($"Manager swap: Next '{nMan}' -> Active '{aMan}' -> Prev '{pMan}'");
                }
                // When player is loading, but stopped, force stop any loading and prioritize the player swap
                if (loading) if (currentState == STOPPED) _Stop();
                prevManager = activeManager;
                activeManager = nextManager;
                if (prevManager == null)
                    prevManager = activeManager;
                hasActiveManager = true;
                activeInitialized = true;
                refreshAfterWait = false; // if both options are active, disable the wait reload to prevent duplicate refreshingif (!tvInit)
                if (!tvInit)
                {
                    tvInit = true;
                    forwardEvent(EVENT_READY);
                }
                
                if (!loading) _RefreshMedia();
                return; // video has been refreshed, hold till next Update call
            }
            else if (refreshAfterWait)
            {
                log("Refresh video via update local");
                refreshAfterWait = false;
                // For some reason when rate limiting happens, the auto reload causes loading to be enabled unexpectely
                // This might cause unexpected edgecases at some point. Keep a close eye on media change issues related to loading states.
                loading = false;
                _RefreshMedia();
                return;
            }
            // activeManager has not been fully init'd yet, skip current cycle.
            if (activeInitialized) { } else return;
            // when video player is switching (as denoted by the epsilon jump time), use the prevManager reference.
            BaseVRCVideoPlayer vp = jumpToTime == EPSILON ? prevManager.videoPlayer : activeManager.videoPlayer;
            if (VRC.SDKBase.Utilities.IsValid(vp)) { } else return; // video player has been unloaded, game is closing
            if (mediaEnded) { } // media has ended, nothing to skip and time doesn't need to be updated
            else if (syncTime == Mathf.Infinity)
            {
                // owner wishes to skip the current media
                endMedia(vp);
                return;
            }
            else
            {
                currentTime = vp.GetTime();
                mediaEnded = currentTime + 0.05f >= endTime;
            }

            if (errorOccurred) return; // blocking error has occurred 

            if (vp.IsPlaying && time > autoSyncWait)
            {
                // every so often trigger an automatic resync
                enforceSyncTime = true;
                autoSyncWait = time + automaticResyncInterval;
            }

            mediaSyncCheck(time, vp);
            // live media does not do time checks for ending.
            if (isLive) return;
            mediaEndCheck(vp);
        }

        private void mediaSyncCheck(float time, BaseVRCVideoPlayer vp)
        {
            if (syncToOwner && !isLive) { }
            else
            {
                // if TV is local or if livestream detected, do sync check logic and skip the rest
                if (enforceSyncTime && time >= syncEnforceWait)
                {
                    // single time update for local-only mode.
                    // Also helps fix audio/video desync/drift in most cases.
                    enforceSyncTime = false;
                    log($"Sync enforcement requested for live or local. Updating to {currentTime}");
                    if (isLive) currentTime += 10f;
                    else currentTime += 0.05f; // anything smaller than this won't be registered by AVPro for some reason...
                    vp.SetTime(currentTime);
                }
                return;
            }

            // This handles updating the sync time from owner to others.
            // owner must be playing and local must not be stopped
            if (isOwner)
            {
                if (enforceSyncTime)
                {
                    if (time >= syncEnforceWait)
                    {
                        // single time update for owner. 
                        // Also helps fix audio/video desync/drift in most cases.
                        enforceSyncTime = false;
                        log($"Sync enforcement requested for owner. Updating to {currentTime}");
                        currentTime += 0.05f; // anything smaller than this won't be registered by AVPro for some reason...
                        vp.SetTime(currentTime);
                    }
                }
                syncTime = currentTime;
            }
            else if (loading) // skip other time checks if TV is loading a video
            {
                // check for loading timeout and trigger PlayerError if exceeded
                // Skip check if a delayed refresh is active
                if (!refreshAfterWait && time >= loadingWait)
                {
                    _OnVideoPlayerError(VideoError.PlayerError);
                }
            }
            else if (currentState != STOPPED && !ownerError)
            {
                var compSyncTime = syncTime + lagComp;
                if (compSyncTime > endTime) compSyncTime = endTime;
                float syncDelta = Mathf.Abs(currentTime - compSyncTime);
                if (currentState == PLAYING)
                {
                    // sync time enforcement check should ONLY be for when the video is playing
                    // Also helps fix audio/video desync/drift in most cases.
                    if (enforceSyncTime)
                    {
                        if (time >= syncEnforceWait)
                        {
                            currentTime = compSyncTime;
                            log($"Sync enforcement requested. Updating to {compSyncTime}");
                            vp.SetTime(currentTime);
                            enforceSyncTime = false;
                        }
                    }
                }
                // video sync enforcement will always occur for paused mode as the user expects the video to not be active, so we can skip forward as needed.
                else if (syncDelta > pausedResyncThreshold)
                {
                    log($"Paused sync threshold exceeded. Updating to {compSyncTime}");
                    currentTime = compSyncTime;
                    vp.SetTime(currentTime);
                }
            }
        }

        private void mediaEndCheck(BaseVRCVideoPlayer vp)
        {
            // loop/media end check
            if (mediaEnded)
            {
                bool shouldLoop = loop > 0;
                if (isOwner && shouldLoop)
                {
                    // owner when loop is active
                    vp.SetTime(startTime);
                    mediaEnded = false;
                    syncTime = currentTime = startTime;
                    forwardEvent(EVENT_MEDIALOOP);
                    if (loop < int.MaxValue)
                    {
                        loop--;
                        if (loop == 0) forwardEvent(EVENT_DISABLELOOP);
                    }
                }
                else if (currentState == PLAYING && endTime > 0f)
                {
                    if (shouldLoop)
                    {

                        if (syncToOwner && syncTime >= currentTime)
                        {
                            log("sync is enabled but sync time hasn't been passed, skip");
                        } // sync is enabled but sync time hasn't been passed, skip
                        else
                        {
                            // non-owner when owner has loop (causing the sync time to start over)
                            vp.SetTime(startTime);
                            mediaEnded = false;
                            // update current time to start time so this only executes once, prevents accidental spam
                            currentTime = startTime;
                            forwardEvent(EVENT_MEDIALOOP);
                            if (loop < int.MaxValue)
                            {
                                loop--;
                                if (loop == 0) forwardEvent(EVENT_DISABLELOOP);
                            }
                        }
                    }
                    else if (!manualLoop)
                    {
                        endMedia(vp);
                        if (isOwner) syncTime = currentTime;
                    }
                }
            }
            else if (manualLoop) manualLoop = false;
        }

        public void _OnVideoPlayerEnd()
        {
            mediaEnded = true;
        }

        private void endMedia(BaseVRCVideoPlayer vp)
        {
            // in any other condition, pause the video, specifying the media has finished
            vp.Pause();
            vp.SetTime(endTime);
            currentState = PAUSED;
            currentTime = syncTime = endTime;
            // once media has finished, any reload info  should be zeroed
            jumpToTime = 0f;
            reloadCache = 0f;
            forwardEvent(EVENT_PAUSE);
            forwardEvent(EVENT_MEDIAEND);
        }

        new void OnPreSerialization()
        {
            lagCompSync = Networking.GetServerTimeInMilliseconds();
        }

        new void OnDeserialization()
        {
            lagComp = (Networking.GetServerTimeInMilliseconds() - lagCompSync) * 0.001f;
        }

        new void OnOwnershipTransferred(VRCPlayerApi p)
        {
            if (isOwner) syncData._RequestSync();
            forwardVariable(OUT_OWNER, p.playerId);
            forwardEvent(EVENT_OWNERCHANGE);
        }

        public void _PostDeserialization()
        {
            if (syncToOwner) { } else return;
            if (syncVideoPlayerSelection && videoPlayer != videoPlayerSync)
            {
                log($"Video Player swap via deserialization {videoPlayer} -> {videoPlayerSync}");
                videoPlayer = videoPlayerSync;
                changeVideoPlayer();
            }
            if (locked != lockedSync)
            {
                log($"Lock change via deserialization {locked} -> {lockedSync}");
                locked = lockedSync;
                lockedByInstanceOwner = locked && Networking.GetOwner(gameObject).isInstanceOwner;
                forwardEvent(locked ? EVENT_LOCK : EVENT_UNLOCK);
            }
            if (ownerError != ownerErrorSync)
            {
                ownerError = ownerErrorSync;
                if (ownerError)
                {
                    warn("Current TV Owner has an error. Media timestamp will not sync until the owner no longer has an error.");
                    return; // do not change local state if owner has error
                }
            }
            if (urlRevision != urlRevisionSync)
            {
                log($"URL change via deserialization {urlRevision} -> {urlRevisionSync}");
                urlRevision = urlRevisionSync;
                queueRefresh(0f);
            }
            if (loading) return; // do not allow certain actions to occur while a video is loading
            // late join first deserialization, wait until initial wait time is complete
            if (Time.realtimeSinceStartup < waitUntil) return;

            if (state != stateSync)
            {
                log($"State change via deserialization {state} -> {stateSync}");
                state = stateSync;
                switch (state)
                {
                    // always enforce stopping
                    case STOPPED:
                        _Stop();
                        break;
                    // allow the local player to be paused if owner is playing
                    case PLAYING:
                        if (locallyPaused) { }
                        else _Play();
                        break;
                    // pause for the local player
                    case PAUSED:
                        // the owner should not be able to trigger the locallyPaused
                        // flag, so use the internal pause method instead of the public
                        // _Pause event.
                        pause();
                        break;
                    default: break;
                }
            }
            // Give the syncTime enough time to catch up before running the time sync logic
            // This mitigates certain issues when switching videos
            queueSync(0.3f);
        }


        // === VideoManager events ===

        public void _OnVideoPlayerError(VideoError error)
        {
            errorOccurred = true;
            err($"Video Error: {error}");
            if (error == VideoError.RateLimited)
            {
                warn("Refresh via rate limit error, retrying in 5 seconds...");
                errorOccurred = false; // skip error shortcircut and use a time check instead
                retrying = true;
                queueRefresh(6f); // an extra second just to avoid any race condition issues with the global rate limit
            }
            else if (error == VideoError.PlayerError || error == VideoError.InvalidURL)
            {
                if (error == VideoError.InvalidURL)
                {
                    if (isLive) warn("Stream is either offline or the URL is incorrect.");
                    warn("Unable to load. Media maybe unavailable, protected, region-locked or the URL is wrong.");
                }
                else if (error == VideoError.PlayerError)
                {
                    if (isLive) warn("Livestream has stopped.");
                    else warn("Unexpected error with the media playback.");
                }

                if (retryCount > 0)
                {
                    errorOccurred = false;
                    // the first retry should be very short.
                    float retryDelay = 6f;
                    // any subsequent retries (meaning 2 or more times the url failed to load) use the repeating delay value
                    if (retrying) retryDelay = repeatingRetryDelay;
                    else retrying = true;
                    // do not decrement retry count if count is "effectively infinite"
                    if (retryCount < int.MaxValue)
                    {
                        retryCount--;
                        log($"{retryCount} retries remaining.");
                    }
                    currentState = PAUSED;
                    // if flag is enabled, flip flop the useAlternateUrl flag once.
                    // if that again fails, flip flop once more and then don't flip any more until success or a new URL is input
                    if (retryUsingAlternateUrl)
                        if (retryingWithAlt && retriedWithAlt) { }
                        else
                        {
                            useAlternateUrl = !useAlternateUrl;
                            if (retryingWithAlt) retriedWithAlt = true;
                            else retryingWithAlt = true;
                        }
                    queueRefresh(retryDelay);
                }
                setLoadingState(false);
            }
            else setLoadingState(false);
            if (isOwner) syncData._RequestSync();
            forwardVariable(OUT_ERROR, error);
            forwardEvent(EVENT_VIDEOPLAYERERROR);
        }


        // Once the active manager detects the player has finished loading, get video information and log
        public void _OnVideoPlayerReady()
        {
            // general media info
            mediaLength = activeManager.videoPlayer.GetDuration();
            isLive = mediaLength == Mathf.Infinity || mediaLength == 0f;
            mediaHash = url.Get().GetHashCode();
            mediaEnded = false;

            if (isLive)
            {
                // livestreams should just start immediately
                prepareMedia();
                startMedia();
            }
            else if (buffering)
            {
                // Event was called buffer flag is set. Buffering has ended
                log("Buffering complete.");
                buffering = false;
                startMedia();
            }
            else if (bufferDelayAfterLoad > 0)
            {
                log($"Allowing video to buffer for {bufferDelayAfterLoad} seconds.");
                // timeout is exceeded while the buffer flag is unset. Buffering has started, call delayed event
                buffering = true;
                prepareMedia();
                SendCustomEventDelayedSeconds(nameof(_OnVideoPlayerReady), bufferDelayAfterLoad);
                // queue up a force resync to help avoid some a/v desync
                queueSync(bufferDelayAfterLoad + 3f);
            }
            else
            {
                prepareMedia();
                startMedia();
            }
        }

        private void prepareMedia()
        {
            cacheMediaStartInfo();
            if (activeManager.isVisible) { }
            else if (manuallyHidden) { }
            else activeManager._Show();

            if (loading)
            {
                if (prevManager != activeManager)
                {
                    if (prevManager != null)
                    {
                        prevManager._ApplyStateTo(activeManager);
                        log($"Hiding previous manager {prevManager.gameObject.name}");
                        prevManager._Stop();
                        forwardVariable(OUT_VOLUME, activeManager.volume);
                        forwardEvent(EVENT_VOLUMECHANGE);
                        forwardEvent(activeManager.audio3d ? EVENT_AUDIOMODE3D : EVENT_AUDIOMODE2D);
                        forwardEvent(activeManager.muted ? EVENT_MUTE : EVENT_UNMUTE);
                    }
                    prevManager = activeManager;
                }
                if (jumpToTime == EPSILON)
                {
                    // If jumptime is still epsilon, a non-switching reload occurred. Calculate new jump time, including buffer delay.
                    var diff = Time.realtimeSinceStartup - reloadStart;
                    jumpToTime = reloadCache + diff + bufferDelayAfterLoad;
                    if (jumpToTime > endTime) jumpToTime = endTime;
                }
                log($"[{activeManager.gameObject.name}] Now Playing: {url}");
                string urlStr = url.Get();
                // caution: slightly cursed. It works only because we only care about the url domain.
                localLabel = getUrlDomain(getUrlQueryParam(urlStr, "url"));
                // simply fall back to the regular domain if the query param isn't present
                if (localLabel == EMPTY) localLabel = getUrlDomain(urlStr);
                forwardEvent(EVENT_MEDIASTART);
            }
            if (mute) { }
            else if (manuallyHidden) { }
            else activeManager._UnMute();
            if (endTime <= startTime)
            {
                log("endTime preceeds startTime. Updating.");
                startTime = 0f; // invalid start time given, zero-out
            }
            if (jumpToTime < startTime)
            {
                log("jumpToTime preceeds startTime. Updating.");
                jumpToTime = startTime;
            }

            if (jumpToTime > 0f)
            {
                log($"Jumping [{activeManager.gameObject.name}] to timestamp: {jumpToTime}");
                activeManager.videoPlayer.SetTime(jumpToTime);
                jumpToTime = 0f;
            }
            videoLoaded = true;
            errorOccurred = false;
            locallyPaused = false;
            // clear the retry flags on successful video load
            retrying = false;
            retryingWithAlt = false;
            retriedWithAlt = false;
            if (isOwner) ownerError = false;
        }

        private void startMedia()
        {
            if (loading)
            {
                if (isOwner)
                {
                    stateSync = playVideoAfterLoad ? PLAYING : PAUSED;
                    syncData._RequestSync();
                }
                setLoadingState(false);
            }

            state = stateSync;
            if (playVideoAfterLoad && stateSync == PLAYING && !locallyPaused)
            {
                currentState = PLAYING;
                activeManager.videoPlayer.Play();
                forwardEvent(EVENT_PLAY);
            }
            else
            {
                currentState = PAUSED;
                forwardEvent(EVENT_PAUSE);
            }
        }

        private void cacheMediaStartInfo()
        {
            if (isLive)
            {
                startTime = 0f;
                endTime = 0f;
                loop = 0;
                // always have at least 1 retry for any live content
                if (retryCount == 0) retryCount = 1;
                log("Params set after video is ready");
                return;
            }

            // grab parameters
            float value = 0f;
            int check = 0;
            string param = EMPTY;
            string urlStr = url.Get();

            // check for start param
            param = getUrlHashParam("start", EMPTY);
            // backwards compatibility
            if (param == EMPTY) param = getUrlQueryParam(urlStr, "start");
            if (float.TryParse(param, out value)) startTime = value;
            else startTime = 0f;

            // check for end param
            param = getUrlHashParam("end", EMPTY);
            // backwards compatibility
            if (param == EMPTY) param = getUrlQueryParam(urlStr, "end");
            if (isLive) endTime = 0f;
            if (float.TryParse(param, out value)) endTime = value;
            else endTime = mediaLength;
            videoDuration = endTime - startTime;

            // if loop is present without value, default to -1
            param = getUrlHashParam("loop", "-1");
            // backwards compatibility
            bool useQueryParam = param == EMPTY;
            if (useQueryParam) param = getUrlQueryParam(urlStr, "loop");
            int.TryParse(param, out check);
            bool oldState = loop != 0;
            bool newState = check != 0;
            loop = check;
            // old query param behaviour for backwards compatibility
            if (useQueryParam && loop == 1) loop = int.MaxValue;
            if (loop < 0) loop = int.MaxValue;
            if (oldState != newState)
                forwardEvent(loop > 0 ? EVENT_ENABLELOOP : EVENT_DISABLELOOP);

            // check for t or start params, only update jumpToTime if start or t succeeds
            // only parse if another jumpToTime value has not been set.
            if (jumpToTime == startTime)
            {
                param = getUrlHashParam("t", EMPTY);
                if (param == EMPTY) param = getUrlQueryParam(urlStr, "t");
                if (float.TryParse(param, out value)) jumpToTime = value;
            }

            log("Params set after video is ready");
        }

        private void cacheMediaChangeInfo()
        {
            string param = EMPTY;
            // if retry is present without value, default to -1
            param = getUrlHashParam("retry", "-1");
            int value;
            if (int.TryParse(param, out value)) retryCount = value;
            if (retryCount < 0) retryCount = int.MaxValue;

            isLive = hasUrlHashParam("live");
        }

        // === Public events to control the TV from user interfaces ===

        [PublicAPI]
        public void _RefreshMedia()
        {
            if (init) { } else return;
            if (loading)
            {
                warn("Cannot change to another media while loading.");
                return; // disallow refreshing media while TV is loading another video
            }
            if (_IsPrivilegedUser())
            {
                // if the owner has an error, take back ownership to fix sync
                if (ownerError) takeOwnership();
            }
            else if (locked)
            {
                // if TV is locked without being privileged, force unset any requested URLs
                // This converts the command into a simple video refresh
                warn("TV is locked. Cannot change media for un-privileged users.");
                IN_URL = VRCUrl.Empty;
                IN_ALT = VRCUrl.Empty;
            }
            // compare input URL and previous URL
            if (IN_URL == null) IN_URL = VRCUrl.Empty;
            if (IN_ALT == null) IN_ALT = VRCUrl.Empty;
            string urlMainStr = IN_URL.Get();
            string urlAltStr = IN_ALT.Get();
            bool newMainUrl = urlMainStr != EMPTY && urlMainStr != urlMain.Get();
            bool newAltUrl = urlAltStr != EMPTY && urlAltStr != urlAlt.Get();
            if (newMainUrl || newAltUrl)
            {
                // when new URls are detected, grab ownership to handle the sync data
                takeOwnership();
                syncData._RequestSync();
                // update relavent URL data
                urlMainSync = urlMain = IN_URL;
                urlAltSync = urlAlt = IN_ALT;
                urlRevision++;
                urlRevisionSync = urlRevision;
                // reset the alternate URL flag back to default
                useAlternateUrl = isQuest;
                // new URL, reset the retry flags
                retrying = false;
                retryingWithAlt = false;
                retriedWithAlt = false;
            }
            // URLs are not changing, thus a reload is taking place
            else if (currentState != STOPPED)
            {
                reloadStart = Time.realtimeSinceStartup;
                // if epsilon is set prior to this point, there is a video swap going on. Use previous manager time instead.
                if (jumpToTime == EPSILON)
                    reloadCache = prevManager.videoPlayer.GetTime();
                else reloadCache = activeManager.videoPlayer.GetTime();
                // skip timestamp continuity if the video is a retry attempt after an error
                if (retrying) jumpToTime = 0f;
                else jumpToTime = EPSILON;
            }
            IN_URL = VRCUrl.Empty;
            IN_ALT = VRCUrl.Empty;

            if (syncToOwner)
            {
                urlMain = urlMainSync;
                urlAlt = urlAltSync;
            }
            urlMainStr = urlMain.Get();
            urlAltStr = urlAlt.Get();

            // sanity checks
            if (urlMainStr == null)
            {
                urlMain = VRCUrl.Empty;
                urlMainStr = EMPTY;
            }
            if (urlAltStr == null)
            {
                urlAlt = VRCUrl.Empty;
                urlAltStr = EMPTY;
            }

            // graceful fallback checks
            if (urlAltStr == EMPTY)
            {
                urlAlt = urlMain;
                urlAltStr = urlMainStr;
            }
            if (urlMainStr == EMPTY)
            {
                urlMain = urlAlt;
                urlMainStr = urlAltStr;
            }

            if (urlMainStr == EMPTY)
            {
                log("No URLs present. Skip.");
                return;
            }
            url = useAlternateUrl ? urlAlt : urlMain;
            bool willLoadDifferentVideo = url.Get().GetHashCode() != mediaHash;
            log($"[{nextManager.gameObject.name}] loading URL: {url}");
            if (stopMediaWhenHidden && manuallyHidden) return; // skip URL loading if media is force hidden
            if (!useAlternateUrl && newAltUrl && !newMainUrl) { }
            else
            {
                // loading state MUST be set first so that any errors that occur (notably the INVALID_URL error)
                // will be able to decide whether to not to change the loading state as needed.
                setLoadingState(true);
                refreshAfterWait = false; // halt any queued refreshes
                videoLoaded = false;
                parseUrlHashParams(url.Get());
                // only cache once per url
                if (!retrying && willLoadDifferentVideo)
                {
                    retryCount = defaultRetryCount;
                    if (retryUsingAlternateUrl)
                        if (retryCount == 0)
                            if (urlMainStr != urlAltStr)
                                retryCount = 1;
                    cacheMediaChangeInfo();
                }
                nextManager.videoPlayer.LoadURL(url);
                nextUrlAttemptAllowed = Time.realtimeSinceStartup + 6f;
            }
            if (willLoadDifferentVideo) forwardEvent(EVENT_MEDIACHANGE);
        }

        [Obsolete]
        public bool _CanTakeControl() => _IsPrivilegedUser();
        [PublicAPI]
        public bool _IsPrivilegedUser()
        {
            if (hasLocalPlayer) { }
            else // ensure local player has been cached.
            {
                localPlayer = Networking.LocalPlayer;
                hasLocalPlayer = true;
            }
            if (hasLocalPlayer)
            {
                string details = "";
                bool allow = !syncToOwner;
                if (debug) details = $"Is the TV not syncing to the owner? {allow}\n";
                if (!allow)
                {
                    allow = localPlayer.isMaster;
                    if (debug) details = $"Is the user the Master? {allow}\n";
                }
                if (allow)
                {
                    allow = allowMasterControl;
                    if (debug) details += $"And is master control enabled? {allow}\n";
                }
                if (allow)
                {
                    allow = !lockedByInstanceOwner;
                    if (debug) details += $"And TV isn't locked by instance owner? {allow}\n";
                }
                if (!allow)
                {
                    cacheSyncData();
                    allow = syncData._CheckWhitelist();
                    if (debug) details += $"Is the user on the whitelist? {allow}\n";
                }
                if (!allow)
                {
                    allow = localPlayer.isInstanceOwner;
                    if (debug) details += $"Is user is instance owner? {allow}\n";
                }
                if (debug)
                {
                    details = $"Is the user privileged enough? {allow}\n" + details;
                    log(details);
                }
                return allow;
            }
            log("No local player available.");
            return false;
        }


        public void _ChangeMedia() => _RefreshMedia();

        // equivalent to: udonBehavior.SetProgramVariable("IN_URL", (VRCUrl) url); udonBehavior.SendCustomEvent("_RefreshMedia");
        [PublicAPI]
        public void _ChangeMediaTo(VRCUrl url)
        {
            IN_URL = url;
            _RefreshMedia();
        }

        [PublicAPI]
        public void _ChangeAltMediaTo(VRCUrl alt)
        {
            IN_ALT = alt;
            _RefreshMedia();
        }

        [PublicAPI]
        public void _ChangeMediaWithAltTo(VRCUrl url, VRCUrl alt)
        {
            IN_URL = url;
            IN_ALT = alt;
            _RefreshMedia();
        }

        [PublicAPI]
        public void _DelayedChangeMediaTo(VRCUrl url)
        {
            IN_URL = url;
            // refresh next frame
            queueRefresh(0f);
        }

        [PublicAPI]
        public void _DelayedChangeMediaWithAltTo(VRCUrl url, VRCUrl alt)
        {
            IN_URL = url;
            IN_ALT = alt;
            queueRefresh(0f);
        }

        // equivalent to: udonBehavior.SetProgramVariable("IN_VIDEOPLAYER", (int) index); udonBehavior.SendCustomEvent("_ChangeVideoPlayer");
        [PublicAPI]
        public void _ChangeVideoPlayer()
        {
            if (init) { } else return;
            // no need to change if same is picked
            if (IN_VIDEOPLAYER == videoPlayer) return;
            // do not allow changing resolution while a video is loading.
            if (loading)
            {
                IN_VIDEOPLAYER = videoPlayer;
                return;
            }
            if (IN_VIDEOPLAYER >= videoManagers.Length)
            {
                err($"Video Player swap value too large: Expected value between 0 and {videoManagers.Length - 1} - Actual {IN_VIDEOPLAYER}");
                return;
            }
            // special condition for time jump between switching video players
            if (hasActiveManager) jumpToTime = EPSILON;
            bool changed = false;
            if (syncToOwner && syncVideoPlayerSelection)
            {
                // if sync flags are enabled, only allow owner to change the video player selection
                if (isOwner)
                {
                    // do the logic
                    changed = true;
                    videoPlayer = IN_VIDEOPLAYER;
                    videoPlayerSync = videoPlayer;
                    syncData._RequestSync();
                }
            }
            else
            {
                // if either sync is disabled, treat the video player swap as local only
                changed = true;
                videoPlayer = IN_VIDEOPLAYER;
            }
            changeVideoPlayer();
            if (changed) log($"Switching to: [{nextManager.gameObject.name}]");
            IN_VIDEOPLAYER = -1;
        }

        private void changeVideoPlayer()
        {
            nextManager = videoManagers[videoPlayer];
            jumpToTime = EPSILON;
            forwardVariable(OUT_VIDEOPLAYER, videoPlayer);
            forwardEvent(EVENT_VIDEOPLAYERCHANGE);
        }

        [PublicAPI]
        public void _ChangeVideoPlayerTo(int videoPlayer)
        {
            IN_VIDEOPLAYER = videoPlayer;
            _ChangeVideoPlayer();
        }

        [PublicAPI]
        public void _TogglePlay()
        {
            if (currentState == PLAYING) _Pause();
            else if (currentState == PAUSED) _Play();
            else if (currentState == STOPPED) _RefreshMedia();
        }

        [PublicAPI]
        public void _Play()
        {
            if (init) { } else return;
            if (currentState == STOPPED)
            {
                log("Refresh video via Play");
                _RefreshMedia();
                return;
            }
            // if owner is paused, prevent non-owner from playing video if they are syncing to owner
            if (syncToOwner && state == PAUSED && !isOwner) return;
            var vp = hasActiveManager ? activeManager.videoPlayer : nextManager.videoPlayer;
            if (isOwner)
            {
                stateSync = state = PLAYING;
                syncData._RequestSync();
            }
            // if media is at end and user forces play, force loop the media one time if the media isn't in a stopped state
            if (mediaEnded && currentState != STOPPED)
            {
                manualLoop = true;
                currentTime = syncTime = startTime;
                vp.SetTime(startTime);
                forwardEvent(EVENT_MEDIALOOP);
            }
            vp.Play();
            currentState = PLAYING;
            locallyPaused = false;
            queueSync(0.2f);
            forwardEvent(EVENT_PLAY);
        }

        [PublicAPI]
        public void _Pause()
        {
            locallyPaused = true; // flag to determine if pause was locally triggered
            pause();
        }
        private void pause()
        {
            if (init) { } else return;
            if (currentState == STOPPED) return; // nothing to pause
            var vp = hasActiveManager ? activeManager.videoPlayer : nextManager.videoPlayer;
            vp.Pause();
            if (isOwner)
            {
                stateSync = state = PAUSED;
                syncData._RequestSync();
            }
            currentState = PAUSED;
            forwardEvent(EVENT_PAUSE);
        }

        [PublicAPI]
        public void _Stop()
        {
            if (!hasActiveManager) return;
            log("Stopping, hiding active");
            if (loading)
            {
                log("Stop called while loading");
                // if stop is called while loading a video, the video loading will be halted instead of the active player
                if (!errorOccurred) nextManager._Stop();
                loading = false;
                locallyPaused = false;
                errorOccurred = false;
                forwardEvent(EVENT_LOADINGSTOP);
                return;
            }
            activeManager._Stop();
            if (isOwner)
            {
                stateSync = state = STOPPED;
                activeManager.videoPlayer.Stop();
                activeManager.videoPlayer.SetTime(0f);
                syncData._RequestSync();
            }
            currentState = STOPPED;
            setLoadingState(false);
            locallyPaused = false;
            refreshAfterWait = false; // halt any queued refreshes
            retryCount = 0;
            forwardEvent(EVENT_STOP);
            errorOccurred = false;
        }

        public void ALL_OwnerEnabled()
        {
            ownerError = ownerErrorSync;
        }

        public void ALL_OwnerDisabled()
        {
            ownerError = true;
        }

        [PublicAPI]
        public void _Skip()
        {
            if (!hasActiveManager) return;
            if (mediaEnded) return; // nothing to skip currently
            if (isOwner)
            {
                syncTime = Mathf.Infinity;
                // trigger a force resync after enough time has passed for sync time to propogate via continuous sync
                queueSync(0.5f);
                syncData.SendCustomEventDelayedSeconds(nameof(TVManagerV2ManualSync._RequestSync), 0.5f);
            }
            else if (_IsPrivilegedUser())
            {
                takeOwnership();
                syncTime = Mathf.Infinity;
                // trigger a force resync after enough time has passed for sync time to propogate via continuous sync
                queueSync(0.5f);
                syncData.SendCustomEventDelayedSeconds(nameof(TVManagerV2ManualSync._RequestSync), 0.5f);
            }
            // else user doesn't have enough privilege
        }

        [PublicAPI]
        public void _Hide()
        {
            if (!hasActiveManager) return;
            if (stopMediaWhenHidden)
            {

                if (isOwner) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerDisabled));
                _Stop();
            }
            else activeManager._Hide();
            manuallyHidden = true;
        }

        [PublicAPI]
        public void _Show()
        {
            if (!hasActiveManager) return;
            manuallyHidden = false;
            if (stopMediaWhenHidden)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_OwnerEnabled));
                _Play();
            }
            else activeManager._Show();
        }

        [PublicAPI]
        public void _ToggleHidden()
        {
            if (manuallyHidden) _Show();
            else _Hide();
        }

        [PublicAPI]
        public void _UseMainUrl()
        {
            useAlternateUrl = false;
            queueRefresh(0f);
        }

        [Obsolete] public void _UseAltUrl() => _UseQuestUrl();

        [PublicAPI]
        public void _UseQuestUrl()
        {
            useAlternateUrl = true;
            queueRefresh(0f);
        }

        [PublicAPI]
        public void _ToggleUrl()
        {
            useAlternateUrl = !useAlternateUrl;
            queueRefresh(0f);
        }

        [PublicAPI]
        public void _Mute()
        {
            if (!hasActiveManager) return;
            mute = true;
            activeManager._Mute();
            forwardEvent(EVENT_MUTE);
        }
        [PublicAPI]
        public void _UnMute()
        {
            if (!hasActiveManager) return;
            mute = false;
            activeManager._UnMute();
            forwardEvent(EVENT_UNMUTE);
        }
        [PublicAPI]
        public void _ToggleMute()
        {
            if (!hasActiveManager) return;
            mute = !mute;
            activeManager._ChangeMute(mute);
            forwardEvent(mute ? EVENT_MUTE : EVENT_UNMUTE);
        }
        [PublicAPI]
        public void _ChangeMuteTo(bool mute)
        {
            if (!hasActiveManager) return;
            this.mute = mute;
            activeManager._ChangeMute(mute);
            forwardEvent(mute ? EVENT_MUTE : EVENT_UNMUTE);
        }
        [PublicAPI]
        public void _ChangeVolume()
        {
            if (!hasActiveManager) return;
            volume = IN_VOLUME;
            activeManager._ChangeVolume(volume);
            forwardVariable(OUT_VOLUME, volume);
            forwardEvent(EVENT_VOLUMECHANGE);
            IN_VOLUME = 0f;
        }
        // equivalent to: udonBehavior.SetProgramVariable("IN_VOLUME", (float) volumePercent); udonBehavior.SendCustomEvent("_ChangeVolume");
        [PublicAPI]
        public void _ChangeVolumeTo(float volume)
        {
            IN_VOLUME = volume;
            _ChangeVolume();
        }
        [PublicAPI]
        public void _AudioMode3d()
        {
            audio3d = true;
            if (hasActiveManager) activeManager._Use3dAudio();
            else nextManager._Use3dAudio();
            forwardEvent(EVENT_AUDIOMODE3D);
        }
        [PublicAPI]
        public void _AudioMode2d()
        {
            audio3d = false;
            if (hasActiveManager) activeManager._Use2dAudio();
            else nextManager._Use2dAudio();
            forwardEvent(EVENT_AUDIOMODE2D);
        }
        [PublicAPI]
        public void _ChangeAudioModeTo(bool audio3d)
        {
            this.audio3d = audio3d;
            if (hasActiveManager) activeManager._ChangeAudioMode(audio3d);
            else nextManager._ChangeAudioMode(audio3d);
            forwardEvent(audio3d ? EVENT_AUDIOMODE3D : EVENT_AUDIOMODE2D);
        }
        [PublicAPI]
        public void _ToggleAudioMode()
        {
            _ChangeAudioModeTo(!audio3d);
        }

        [PublicAPI]
        public void _ReSync()
        {
            if (syncToOwner) queueSync(0f);
        }
        [PublicAPI]
        public void _Sync()
        {
            syncToOwner = true;
            enforceSyncTime = true;
            forwardEvent(EVENT_SYNC);
        }
        [PublicAPI]
        public void _DeSync()
        {
            syncToOwner = false;
            enforceSyncTime = false;
            forwardEvent(EVENT_DESYNC);
        }
        [PublicAPI]
        public void _ChangeSyncTo(bool sync)
        {
            if (sync) _Sync();
            else _DeSync();
        }
        public void _ToggleSync()
        {
            _ChangeSyncTo(!syncToOwner);
        }

        // equivalent to: udonBehavior.SetProgramVariable("IN_SEEK", (float) seekPercent); udonBehavior.SendCustomEvent("_ChangeSeekTime");
        [PublicAPI]
        public void _ChangeSeekTime()
        {
            if (hasActiveManager) { } else return;
            var vp = activeManager.videoPlayer;
            float dur = vp.GetDuration();
            // inifinty and 0 are livestreams, they cannot adjust seek time.
            if (dur == Mathf.Infinity || dur == 0f) return;
            vp.SetTime(IN_SEEK);
            IN_SEEK = 0f;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_ManualQuickSeek));
        }
        [PublicAPI]
        public void _ChangeSeekTimeTo(float seconds)
        {
            if (seconds > endTime) seconds = endTime;
            if (seconds < startTime) seconds = startTime;
            IN_SEEK = seconds;
            _ChangeSeekTime();
        }

        [PublicAPI]
        public void _ChangeSeekPercent()
        {
            // map the percent value to the range of the start and end time to the the target timestamp
            IN_SEEK = (endTime - startTime) * IN_SEEK + startTime;
            _ChangeSeekTime();
        }
        [PublicAPI]
        public void _ChangeSeekPercentTo(float seekPercent)
        {
            if (seekPercent > 1f) seekPercent = 1f;
            if (seekPercent < 0f) seekPercent = 0f;
            IN_SEEK = seekPercent;
            _ChangeSeekPercent();
        }

        [PublicAPI]
        public void _SeekForward()
        {
            if (!hasActiveManager) return;
            if (isLive) enforceSyncTime = true;
            else _ChangeSeekTimeTo(activeManager.videoPlayer.GetTime() + 10f);
        }
        [PublicAPI]
        public void _SeekBackward()
        {
            if (!hasActiveManager) return;
            if (isLive) enforceSyncTime = true;
            else _ChangeSeekTimeTo(activeManager.videoPlayer.GetTime() - 10f);
        }

        [PublicAPI]
        public void _Lock()
        {
            if (!hasActiveManager) return;
            if (_IsPrivilegedUser())
            {
                lockedByInstanceOwner = localPlayer.isInstanceOwner;
                if (takeOwnership())
                {
                    locked = lockedSync = true;
                    syncData._RequestSync();
                    forwardEvent(EVENT_LOCK);
                }
            }
        }
        [PublicAPI]
        public void _UnLock()
        {
            if (!hasActiveManager) return;
            if (_IsPrivilegedUser())
            {
                lockedByInstanceOwner = false;
                if (takeOwnership())
                {
                    locked = lockedSync = false;
                    syncData._RequestSync();
                    forwardEvent(EVENT_UNLOCK);
                }
            }
        }
        [PublicAPI]
        public void _ChangeLockTo(bool lockActive)
        {
            if (lockActive) _Lock();
            else _UnLock();
        }
        [PublicAPI]
        public void _ToggleLock()
        {
            if (locked) _UnLock();
            else _Lock();
        }

        // Use these methods to globally enable or disable all possible attached interactive UIs for any event listeners attached
        [PublicAPI]
        public void _EnableInteractions() => switchInteractions(true);

        [PublicAPI]
        public void _DisableInteractions() => switchInteractions(false);

        [PublicAPI]
        public void _ToggleInteractions() => switchInteractions(!interactionState);

        private void switchInteractions(bool newState)
        {
            foreach (Component target in eventTargets)
            {
                if (target == null) continue;
                var uiShapes = target.gameObject.GetComponentsInChildren(typeof(VRC_UiShape), true);
                foreach (Component uiShape in uiShapes)
                {
                    var interactables = uiShape.GetComponents<Collider>();
                    foreach (Collider interactable in interactables)
                        interactable.enabled = newState;
                }
            }
            interactionState = newState;
        }

        // Use this method to subscribe to the TV's event forwarding.
        // Useful for attaching multiple control panels or behaviors for various side effects to happen.
        [PublicAPI]
        public void _RegisterUdonEventReceiver()
        {
            if (IN_SUBSCRIBER == null) return; // called without setting the behavior
            sendEvents = true;
            if (eventTargets == null)
            {
                eventTargets = new Component[0];
                skipTargets = new Component[0];
                eventTargetWeights = new byte[0];
            }
            int index = 0;
            for (; index < eventTargetWeights.Length; index++)
            {
                if (IN_PRIORITY < eventTargetWeights[index]) break;
            }
            log($"Expanding event register to {eventTargets.Length + 1}: Adding {IN_SUBSCRIBER.gameObject.name}");
            // eventTargets = (Component[])AddArrayItem(eventTargets, index, IN_SUBSCRIBER);
            // skipTargets = (Component[])AddArrayItem(skipTargets, index, null);
            // eventTargetWeights = (byte[])AddArrayItem(eventTargetWeights, index, IN_PRIORITY);


            var _targets = eventTargets;
            var _skips = skipTargets;
            var _weights = eventTargetWeights;
            eventTargets = new Component[_targets.Length + 1];
            skipTargets = new Component[_skips.Length + 1];
            eventTargetWeights = new byte[_targets.Length + 1];
            int i = 0;
            int offset = 0;
            for (; i < _targets.Length; i++)
            {
                if (i == index) offset = 1;
                eventTargets[i + offset] = _targets[i];
                eventTargetWeights[i + offset] = _weights[i];
            }
            eventTargets[index] = IN_SUBSCRIBER;
            eventTargetWeights[index] = IN_PRIORITY;
            // forward the ready state for the freshly registered subscriber if the TV is already initialized
            if (tvInit)
            {
                IN_SUBSCRIBER.SendCustomEvent(EVENT_READY);
                log($"Forwarding event {EVENT_READY} to 1 listeners");
            }
            IN_SUBSCRIBER = null;
            IN_PRIORITY = defaultPriority;
        }

        [PublicAPI]
        public void _RegisterUdonSharpEventReceiver(UdonSharpBehaviour target)
        {
            IN_SUBSCRIBER = (UdonBehaviour)(Component)target;
            IN_PRIORITY = defaultPriority;
            _RegisterUdonEventReceiver();
        }

        [PublicAPI]
        public void _RegisterUdonSharpEventReceiverWithPriority(UdonSharpBehaviour target, byte priority)
        {
            IN_SUBSCRIBER = (UdonBehaviour)(Component)target;
            IN_PRIORITY = priority;
            _RegisterUdonEventReceiver();
        }

        [PublicAPI]
        public void _UnregisterUdonEventReceiver()
        {
            if (IN_SUBSCRIBER == null) return; // called without setting the behavior
            var index = Array.IndexOf(eventTargets, IN_SUBSCRIBER);
            if (index == -1) index = Array.IndexOf(skipTargets, IN_SUBSCRIBER); // check for if it is disabled instead
            if (index > -1)
            {
                log($"Reducing event register to {eventTargets.Length - 1}: Removing {IN_SUBSCRIBER.gameObject.name}");
                eventTargets = (Component[])RemoveArrayItem(eventTargets, index);
                skipTargets = (Component[])RemoveArrayItem(skipTargets, index);
                eventTargetWeights = (byte[])RemoveArrayItem(eventTargetWeights, index);
            }
            sendEvents = eventTargets.Length > 0;
            IN_SUBSCRIBER = null;
        }

        [PublicAPI]
        public void _UnregisterUdonSharpEventReceiver(UdonSharpBehaviour target)
        {
            IN_SUBSCRIBER = (UdonBehaviour)(Component)target;
            _UnregisterUdonEventReceiver();
        }

        [PublicAPI]
        public void _EnableUdonEventReceiver()
        {
            if (IN_SUBSCRIBER == null) return;
            var index = Array.IndexOf(skipTargets, IN_SUBSCRIBER);
            if (index > -1)
            {
                log($"Enabling subscriber {IN_SUBSCRIBER.gameObject.name}");
                eventTargets[index] = IN_SUBSCRIBER;
                skipTargets[index] = null;
            }
            IN_SUBSCRIBER = null;
        }

        [PublicAPI]
        public void _EnableUdonSharpEventReceiver(UdonSharpBehaviour target)
        {
            IN_SUBSCRIBER = (UdonBehaviour)(Component)target;
            _EnableUdonEventReceiver();
        }

        [PublicAPI]
        public void _DisableUdonEventReceiver()
        {
            if (IN_SUBSCRIBER == null) return;
            var index = Array.IndexOf(eventTargets, IN_SUBSCRIBER);
            if (index > -1)
            {
                log($"Disabling subscriber {IN_SUBSCRIBER.gameObject.name}");
                eventTargets[index] = null;
                skipTargets[index] = IN_SUBSCRIBER;
            }
            IN_SUBSCRIBER = null;
        }

        [PublicAPI]
        public void _DisableUdonSharpEventReceiver(UdonSharpBehaviour target)
        {
            IN_SUBSCRIBER = (UdonBehaviour)(Component)target;
            _DisableUdonEventReceiver();
        }

        [PublicAPI]
        public void _SetUdonSubscriberPriorityToFirst()
        {
            shiftPriority(IN_SUBSCRIBER, 0);
            IN_SUBSCRIBER = null;
        }
        [PublicAPI]
        public void _SetUdonSharpSubscriberPriorityToFirst(UdonSharpBehaviour target) => shiftPriority((UdonBehaviour)(Component)target, 0);

        [PublicAPI]
        public void _SetUdonSubscriberPriorityToHigh()
        {
            shiftPriority(IN_SUBSCRIBER, 1);
            IN_SUBSCRIBER = null;
        }
        [PublicAPI]
        public void _SetUdonSharpSubscriberPriorityToHigh(UdonSharpBehaviour target) => shiftPriority((UdonBehaviour)(Component)target, 1);

        [PublicAPI]
        public void _SetUdonSubscriberPriorityToLow()
        {
            shiftPriority(IN_SUBSCRIBER, 2);
            IN_SUBSCRIBER = null;
        }
        [PublicAPI]
        public void _SetUdonSharpSubscriberPriorityToLow(UdonSharpBehaviour target) => shiftPriority((UdonBehaviour)(Component)target, 2);

        [PublicAPI]
        public void _SetUdonSubscriberPriorityToLast()
        {
            shiftPriority(IN_SUBSCRIBER, 3);
            IN_SUBSCRIBER = null;
        }
        [PublicAPI]
        public void _SetUdonSharpSubscriberPriorityToLast(UdonSharpBehaviour target) => shiftPriority((UdonBehaviour)(Component)target, 3);


        protected System.Array AddArrayItem(System.Array stale, int index, System.Object insert)
        {
            int oldLength = stale.Length;
            int newLength = oldLength + 1;
            if (index > oldLength) index = oldLength; // prevent out of bounds issue
            if (index < 0) index = newLength + index; // enable negative indexes
            System.Array fresh = System.Array.CreateInstance(typeof(System.Object), newLength);
            // copy from start to the index
            System.Array.Copy(stale, 0, fresh, 0, index);
            // if element was anything but the last element, copy the remainder after the index
            if (index < oldLength) System.Array.Copy(stale, index + 1, fresh, index + 1, oldLength - index);
            fresh.SetValue(insert, index);
            return fresh;
        }

        protected System.Array RemoveArrayItem(System.Array stale, int index)
        {
            int oldLength = stale.Length;
            if (oldLength == 0) return stale; // nothing to remove
            int newLength = oldLength - 1;
            System.Array fresh = System.Array.CreateInstance(typeof(System.Object), newLength);
            if (newLength == 0) return fresh; // new array has nothing to copy
            if (index > newLength) index = newLength; // prevent out of bounds issue
            if (index < 0) index = oldLength + index; // enable negative indexes
            // copy from start to the index
            System.Array.Copy(stale, 0, fresh, 0, index);
            // if element was anything but the last element, copy the remainder after the index
            if (index < newLength) System.Array.Copy(stale, index + 1, fresh, index, newLength - index);
            return fresh;
        }


        private void shiftPriority(UdonBehaviour target, byte mode)
        {
            if (target == null) return;
            if (eventTargets == null)
            {
                eventTargets = new Component[0];
                skipTargets = new Component[0];
                eventTargetWeights = new byte[0];
            }
            int oldIndex = Array.IndexOf(eventTargets, target);
            if (oldIndex == -1) oldIndex = Array.IndexOf(skipTargets, target);
            if (oldIndex == -1)
            {
                err("Unable to find matching subscriber. Please ensure the behaviour has been registered first.");
                return;
            }
            log($"Updating priority for {target.gameObject.name}");
            int newIndex = getNewWeightIndex(oldIndex, mode);
            if (newIndex == oldIndex)
            {
                log("No priority change required. Skipping");
                return;
            }
            // detect left vs right vs no shifting
            int left = oldIndex, right = newIndex, shift = 0;
            if (newIndex < oldIndex)
            {
                left = newIndex;
                right = oldIndex - 1;
                shift = 1;
            }
            else if (oldIndex < newIndex)
            {
                left = oldIndex + 1;
                right = newIndex;
                shift = -1;
            }
            Component t = eventTargets[oldIndex];
            Component s = skipTargets[oldIndex];
            byte w = eventTargetWeights[oldIndex];
            // logBehaviourOrder();
            // shift the elements between the old and new indexes into their new positions
            Array.Copy(eventTargets, left, eventTargets, left + shift, right - left + 1);
            Array.Copy(skipTargets, left, skipTargets, left + shift, right - left + 1);
            Array.Copy(eventTargetWeights, left, eventTargetWeights, left + shift, right - left + 1);
            // update the values for the new index to the values at the old index
            eventTargets[newIndex] = t;
            skipTargets[newIndex] = s;
            if (mode == 0) eventTargetWeights[newIndex] = 0;
            else if (mode == 1 || mode == 2) eventTargetWeights[newIndex] = w;
            else if (mode == 3) eventTargetWeights[newIndex] = 255;
            // logBehaviourOrder();
        }

        private int getNewWeightIndex(int oldIndex, byte mode)
        {
            int len = eventTargetWeights.Length;
            int newIndex = oldIndex;
            // FIRST
            if (mode == 0) newIndex = 0;
            // HIGH
            else if (mode == 1)
            {
                byte weight = eventTargetWeights[oldIndex];
                for (; newIndex > -1; newIndex--)
                    if (eventTargetWeights[newIndex] < weight) break;
                if (newIndex == -1) newIndex = 0; // all weights to the left were the same value. Set to start of array index.
            }
            // LOW
            else if (mode == 2)
            {
                byte weight = eventTargetWeights[oldIndex];
                for (; newIndex < len; newIndex++)
                    if (eventTargetWeights[newIndex] > weight) break;
                if (newIndex == len) newIndex = len - 1; // all weights to the right were the same value. Set to end of array index.
            }
            // LAST
            else if (mode == 3) newIndex = len - 1;
            return newIndex;
        }

        private void logBehaviourOrder()
        {
            string _log = "Priorities: ";
            for (int i = 0; i < eventTargets.Length; i++)
            {
                var e = eventTargets[i];
                if (e == null) continue; // skip disabled subscribers
                var n = e.gameObject.name;
                var p = eventTargetWeights[i];
                _log += $"{n} [{p}], ";
            }
            log(_log);
        }

        private bool takeOwnership()
        {
            if (!init) return false;
            if (Networking.IsOwner(gameObject)) return true; // local already owns the TV
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, syncData.gameObject);
            return true;
        }

        private void _RequestSync()
        {
            if (!init) return;
            if (isOwner) syncData._RequestSync();
        }

        public VideoManagerV2 _GetVideoManager()
        {
            VideoManagerV2 manager = jumpToTime == EPSILON ? prevManager : activeManager;
            if (manager == null) manager = nextManager;
            return manager;
        }

        // Disabled for now as it doesn't work correctly
        // IMPORTANT NOTE: This can be a very expensive operation depending on the original setup.
        public Texture2D _GetVideoTexture()
        {
            //     return _GetVideoManager()._GetVideoTexture();
            return null;
        }

        // === Networked methods ===

        public void ALL_ManualQuickSeek()
        {
            if (!isOwner && syncToOwner) queueSync(1f);
        }

        public void ALL_ManualSeek()
        {
            if (syncToOwner) queueSync(syncEnforcementTimeLimit);
        }


        // === Helper Methods ===

        private void queueRefresh(float time)
        {
            refreshAfterWait = true;
            waitUntil = Time.realtimeSinceStartup + time;
            // ensure that the global rate limit time is respected
            if (waitUntil < nextUrlAttemptAllowed)
                waitUntil = nextUrlAttemptAllowed;
        }

        private void queueSync(float time)
        {
            enforceSyncTime = true;
            syncEnforceWait = Time.realtimeSinceStartup + time;
        }

        private void forwardEvent(string eventName)
        {
            if (sendEvents && haltEvent(eventName))
            {
                int count = 0;
                foreach (var target in eventTargets)
                    if (target != null)
                    {
                        count++;
                        ((UdonBehaviour)target).SendCustomEvent(eventName);
                    }
                log($"Forwarding event {eventName} to {count} listeners");
            }
            releaseEvent(eventName);
        }

        private void forwardVariable(string variableName, object value)
        {
            if (sendEvents && variableName != null)
            {
                int count = 0;
                foreach (var target in eventTargets)
                    if (target != null)
                    {
                        count++;
                        ((UdonBehaviour)target).SetProgramVariable(variableName, value);
                    }
                log($"Forwarding variable {variableName} to {count} listeners");
            }
        }

        // These two methods are used to prevent recursive event propogation between the TV and subscribed behaviors.
        // Only allows for 1 depth of calling an event before releasing from it's own context.
        private bool haltEvent(string eventName)
        {
            int insert = -1;
            for (int i = 0; i < haltedEvents.Length; i++)
            {
                if (haltedEvents[i] == eventName) return false;
                if (insert == -1 && haltedEvents[i] == null) insert = i;
            }
            haltedEvents[insert] = eventName;
            return true;
        }

        private void releaseEvent(string eventName)
        {
            for (int i = 0; i < haltedEvents.Length; i++)
            {
                haltedEvents[i] = null;
            }
        }

        private void setLoadingState(bool yes)
        {
            if (yes)
            {
                loadingWait = Time.realtimeSinceStartup + maxAllowedLoadingTime;
            }
            loading = yes;
            if (isOwner) loadingSync = loading;
            if (loading) forwardEvent(EVENT_LOADING);
            else forwardEvent(EVENT_LOADINGEND);
        }

        private string getUrlDomain(string url)
        {
            // strip the protocol
            var s = url.Split(new string[] { "://" }, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return EMPTY;
            // strip everything after the first slash
            s = s[1].Split(new char[] { '/' }, 2, System.StringSplitOptions.None);
            // just to be sure, strip everything after the question mark if one is present
            s = s[0].Split(new char[] { '?' }, 2, System.StringSplitOptions.None);
            // return the url's domain value
            return s[0];
        }

        private int getUrlParamAsInt(string url, string name, int _default)
        {
            string param = getUrlQueryParam(url, name);
            int value;
            return System.Int32.TryParse(param, out value) ? value : _default;
        }


        private string getUrlQueryParam(string url, string name)
        {
            // strip everything before the query parameters
            string[] s = url.Split(SPLIT_QUERY, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return EMPTY;
            // just to be sure, strip everything after the url bang if one is present
            s = s[1].Split(SPLIT_HASH, 2, System.StringSplitOptions.None);
            // attempt to find parameter name match
            s = s[0].Split(SPLIT_QUERY_PARAM);
            foreach (string param in s)
            {
                string[] p = param.Split(SPLIT_VALUE, 2, System.StringSplitOptions.None);
                if (p[0] == name) return p[1];
            }
            // if one can't be found, return an empty string
            return EMPTY;
        }

        private void parseUrlHashParams(string url)
        {
            string[] hashParams = url.Split(SPLIT_HASH, 2, System.StringSplitOptions.None);
            if (hashParams.Length == 1)
            {
                urlHashParams = new string[0];
                return;
            }
            hashParams = hashParams[1].Split(SPLIT_HASH_PARAM);
            int paramCount = hashParams.Length;
            urlHashParams = new string[paramCount];
            urlHashValues = new string[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                string[] split = hashParams[i].Split(SPLIT_VALUE, 2, System.StringSplitOptions.None);
                urlHashParams[i] = split[0].Trim().ToLower();
                if (split.Length > 1)
                    urlHashValues[i] = split[1].Trim();
                else urlHashValues[i] = EMPTY;
            }
            log(String.Join(", ", urlHashParams));
            log(String.Join(", ", urlHashValues));
        }

        private bool hasUrlHashParam(string name)
        {
            return Array.IndexOf(urlHashParams, name.ToLower()) > -1;
        }

        private string getUrlHashParam(string name, string _default)
        {
            int index = Array.IndexOf(urlHashParams, name.ToLower());
            if (index > -1)
            {
                string val = urlHashValues[index];
                if (val == EMPTY) return _default;
                else return val;
            }
            else return EMPTY;
        }


        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ff00>{nameof(TVManagerV2)} ({name})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ff00>{nameof(TVManagerV2)} ({name})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ff00>{nameof(TVManagerV2)} ({name})</color>] {value}");
        }
    }
}
