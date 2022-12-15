
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class Controls_ActiveState : UdonSharpBehaviour
    {
        public TVManagerV2 tv;
        public Queue queue;
        public bool showVideoOwner = true;
        public bool showVideoOwnerID = false;
        public VRCUrlInputField mainUrlInput;
        public VRCUrlInputField altUrlInput;
        public Button activateUrls;
        [HideInInspector, Obsolete] public Button updateMainUrl;
        public Button switchToMainUrl;
        public Image mainIndicator;
        public Button switchToAltUrl;
        public Image altIndicator;
        public Button play;
        public Button pause;
        public Button stop;
        public Button resync;
        public Button audioMode;
        public Image audioModeIndicator;
        public Sprite audio3d;
        public Sprite audio2d;
        public Button mute;
        public Image muteIndicator;
        public Sprite muted;
        public Sprite unmuted;
        public Button masterLock;
        public Image masterLockIndicator;
        public Sprite lockedIcon;
        public Sprite unlockedIcon;
        public Slider volume;
        private Image volumeIndicator;
        public Sprite volumeHigh;
        public Sprite volumeMed;
        public Sprite volumeLow;
        public Sprite volumeOff;
        public Button syncMode;
        public Image syncModeIndicator;
        public Sprite syncEnforced;
        public Sprite localOnly;
        public Slider seek;
        public Text currentTime;
        public Text endTime;
        public Slider loadingBar;
        public Transform loadingSpinner;
        public GameObject loadingSpinnerContainer;
        public Dropdown videoPlayerSwap;
        public Text info;

        // enum values for state/stateSync/currentState
        private const int STOPPED = 0;
        private const int PLAYING = 1;
        private const int PAUSED = 2;


        private VideoError OUT_ERROR;
        private float OUT_VOLUME;
        private int OUT_VIDEOPLAYER;

        // boolean checks for the existence of the various public fields
        private bool hasQueue;
        private bool hasMainInput;
        private bool hasAltInput;
        private bool hasMain;
        private bool hasAlt;
        private bool hasGo;
        private bool hasAltGo;
        private bool hasPlay;
        private bool hasPause;
        private bool hasStop;
        private bool hasResync;
        private bool hasSync;
        private bool hasAudioMode;
        private bool hasMute;
        private bool hasMasterLock;
        private bool hasSeek;
        private bool hasVolume;
        private bool hasLoadingBar;
        private bool hasLoadingSpinner;
        private bool hasVideoPlayerSwap;
        private bool hasInfo;
        private bool hasCurrentTime;
        private bool hasEndTime;
        private bool hasLocalPlayer;


        private bool needDropdownCorrection = true;
        private float loadingBarDamp = 0f;
        private float startTime = 0f;
        private float duration = 0f;
        private int videoPlayerSwapOrder = 0;
        private bool isLive = true;
        private bool isLoading = false;
        private bool isLocked = false;
        private bool skipSeek = false;
        private bool skipVol = false;
        private bool init = false;
        private bool tvInit = false;
        private string tvName = "No TV Connected";
        private bool debug = false;
        private string debugLabel;
        private VRCPlayerApi localPlayer;

        public void _Initialize()
        {
            if (init) return;
            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            if (tv == null)
            {
                debugLabel = $"<Missing TV Ref>/{name}";
                err("The TV reference was not provided. Please make sure the controls know what TV to connect to.");
                return;
            }
            debugLabel = $"{tv.gameObject.name}/{name}";
            tvName = tv.gameObject.name;
            debug = tv.debug;
            localPlayer = Networking.LocalPlayer;

            if (updateMainUrl != null) activateUrls = updateMainUrl;
            hasQueue = queue != null;
            hasLocalPlayer = localPlayer != null;
            hasMainInput = mainUrlInput != null;
            hasAltInput = altUrlInput != null;
            hasMain = switchToMainUrl != null;
            hasAlt = switchToAltUrl != null;
            hasGo = activateUrls != null;
            hasPlay = play != null;
            hasPause = pause != null;
            hasStop = stop != null;
            hasResync = resync != null;
            hasAudioMode = audioMode != null;
            hasMute = mute != null;
            hasMasterLock = masterLock != null;
            hasSeek = seek != null;
            hasSync = syncMode != null;
            hasVolume = volume != null;
            hasLoadingBar = loadingBar != null;
            hasLoadingSpinner = loadingSpinner != null;
            hasVideoPlayerSwap = videoPlayerSwap != null;
            hasInfo = info != null;
            hasCurrentTime = currentTime != null;
            hasEndTime = endTime != null;

            // hide the go button until text is entered into the input field
            if (hasGo) activateUrls.gameObject.SetActive(false);
            if (hasMain)
            {
                if (mainIndicator == null) mainIndicator = switchToMainUrl.image;
                mainIndicator.enabled = !tv.useAlternateUrl;
            }
            if (hasAlt)
            {
                if (altIndicator == null) altIndicator = switchToAltUrl.image;
                altIndicator.enabled = tv.useAlternateUrl;
            }
            if (hasMute)
            {
                if (muteIndicator == null) muteIndicator = mute.image;
            }
            if (hasAudioMode)
            {
                if (audioModeIndicator == null) audioModeIndicator = audioMode.image;
            }
            if (hasVolume)
            {
                // volume expects the structure of a default Unity UI slider
                var imgs = volume.handleRect.GetComponentsInChildren<Image>();
                foreach (Image img in imgs)
                {
                    if (volumeIndicator == null) volumeIndicator = img;
                    else if (img.name == "Fill") volumeIndicator = img;
                }

                // volumeIndicator = volume.handleRect.GetComponentInChildren<Image>();
            }
            if (hasMasterLock)
            {
                if (masterLockIndicator == null) masterLockIndicator = masterLock.image;
                // masterLock.gameObject.SetActive(tv._IsPrivilegedUser()); // hide by default until later
            }
            if (hasLoadingBar) loadingBar.gameObject.SetActive(false);
            if (hasLoadingSpinner)
            {
                if (loadingSpinnerContainer == null)
                    loadingSpinnerContainer = loadingSpinner.gameObject;
            }
            if (hasSeek)
            {
                skipSeek = true;
                seek.value = 1f / 0f; // hide the seek bar button initially
                skipSeek = false;
            }
            if (hasSync)
            {
                if (syncModeIndicator == null) syncModeIndicator = syncMode.image;
            }
            tv._RegisterUdonSharpEventReceiverWithPriority(this, 130);
            init = true;
        }

        void Start()
        {
            _Initialize();
            if (hasVideoPlayerSwap)
            {
                var c = videoPlayerSwap.transform.Find("Template").GetComponent<Canvas>();
                if (c != null)
                {
                    // cache the original sort order for the template object
                    videoPlayerSwapOrder = c.sortingOrder;
                }
            }
        }

        void LateUpdate() => _InternalUpdate();

        public void _InternalUpdate()
        {
            if (tvInit) { } else return;
            if (isLoading)
            {
                if (hasLoadingSpinner)
                    // rotate the spinner while loading a video
                    loadingSpinner.Rotate(0f, 0f, (-200f * Time.deltaTime) % 360f);

                if (hasLoadingBar)
                {
                    // Loading bar "animation"
                    if (loadingBar.value > 0.95f) return;
                    if (loadingBar.value > 0.8f)
                        loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, 0.4f);
                    else
                        loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, 0.3f);
                }
            }
            float timestamp = tv.currentTime - startTime;
            if (hasSeek)
            {
                if (isLive || duration == 0) { }
                else seek.SetValueWithoutNotify(timestamp / duration); // normalize times to the range of start and end times.
            }
            if (hasCurrentTime)
                currentTime.text = getReadableTime(timestamp);

            if (hasVideoPlayerSwap)
            {
                // This fixes the stupid canvas sorting order 30000 issue for nested canvases cause *Unity*
                var t = videoPlayerSwap.transform.Find("Dropdown List");
                if (t != null)
                {
                    if (needDropdownCorrection)
                    {
                        var c = t.GetComponent<Canvas>();
                        var box = c.GetComponent<BoxCollider>();
                        if (box != null)
                        {
                            var rect = (RectTransform)t;
                            box.isTrigger = true;
                            var sizeDelta = rect.sizeDelta;
                            box.size = new Vector3(sizeDelta.x, sizeDelta.y, 0);
                            box.center = new Vector3(0, sizeDelta.y / 2, 0);
                        }
                        // assign the cached sort order value and move the dropdown in front of the blocker element
                        c.sortingOrder = videoPlayerSwapOrder;
                        var blocker = videoPlayerSwap.transform.parent.Find("Blocker");
                        if (blocker != null)
                        {
                            blocker.GetComponent<Canvas>().sortingOrder = videoPlayerSwapOrder;
                            videoPlayerSwap.transform.SetSiblingIndex(blocker.GetSiblingIndex() + 1);
                        }
                        needDropdownCorrection = false;
                    }
                }
                else needDropdownCorrection = true;
            }
        }

        new void OnPlayerLeft(VRCPlayerApi player)
        {
            if (hasMainInput) if (isTvOwner()) mainUrlInput.gameObject.SetActive(true);
            // if (hasMasterLock) masterLock.gameObject.SetActive(tv._IsPrivilegedUser());
        }
        // === UI EVENTS ===

        public void _UpdateUrlInput()
        {
            if (hasGo)
            {
                bool showGo = false;
                if (hasMainInput) if (mainUrlInput.GetUrl().Get() != string.Empty) showGo = true;
                if (hasAltInput) if (altUrlInput.GetUrl().Get() == string.Empty) showGo = true;
                activateUrls.gameObject.SetActive(showGo);
            }
        }

        public void _UpdateMainUrlInput() => _UpdateUrlInput();

        public void _UpdateAltUrlInput() => _UpdateUrlInput();

        public void _EndEditUrlInput()
        {
            if (Input.GetKey(KeyCode.Return)) _ChangeMedia();
            else if (Input.GetKey(KeyCode.KeypadEnter)) _ChangeMedia();
        }

        public void _EndEditAltUrlInput() => _EndEditUrlInput();

        public void _Play() => tv._Play();
        public void _Pause() => tv._Pause();
        public void _Stop() => tv._Stop();
        public void _ReSync() => tv._ReSync();
        public void _ToggleSync() => tv._ToggleSync();
        public void _RefreshMedia() => tv._RefreshMedia();
        public void _Refresh() => tv._RefreshMedia();
        public void _ToggleAudioMode() => tv._ToggleAudioMode();
        public void _ToggleMute() => tv._ToggleMute();
        public void _ToggleLock() => tv._ToggleLock();
        public void _SeekForward() => tv._SeekForward();
        public void _SeekBackward() => tv._SeekBackward();
        public void _Seek()
        {
            if (hasSeek) if (skipSeek) { } else tv._ChangeSeekPercentTo(seek.value);
        }
        public void _ChangeVolume()
        {
            if (hasVolume) { } else return;
            if (skipVol)
            {
                // prevent the recursive loop
                skipVol = false;
                return;
            }
            if (volume.value != OUT_VOLUME)
            {
                tv._ChangeVolumeTo(volume.value);
                if (volume.value == 0f) volumeIndicator.sprite = volumeOff;
                else if (volume.value > 0.9f) volumeIndicator.sprite = volumeHigh;
                else if (volume.value > 0.4f) volumeIndicator.sprite = volumeMed;
                else volumeIndicator.sprite = volumeLow;
            }
        }
        public void _ChangeVideoPlayer()
        {
            if (hasVideoPlayerSwap) tv._ChangeVideoPlayerTo(videoPlayerSwap.value);
        }
        public void _ChangeMedia() => swapMedia(VRCUrl.Empty, VRCUrl.Empty);
        public void _UpdateMedia() => swapMedia(tv.urlMain, tv.urlAlt);

        private void swapMedia(VRCUrl main, VRCUrl alt)
        {
            bool hasMainUrl = false;
            bool hasAltUrl = false;
            if (hasMainInput)
            {
                VRCUrl _main = mainUrlInput.GetUrl();
                if (_main.Get() != string.Empty)
                {
                    main = _main;
                    hasMainUrl = true;
                    mainUrlInput.SetUrl(VRCUrl.Empty);
                }
            }
            if (hasAltInput)
            {
                VRCUrl _alt = altUrlInput.GetUrl();
                if (_alt.Get() != string.Empty)
                {
                    alt = _alt;
                    hasAltUrl = true;
                    altUrlInput.SetUrl(VRCUrl.Empty);
                }
            }
            if (hasMainUrl || hasAltUrl)
            {
                if (hasQueue)
                {
                    if (hasMainUrl) queue.IN_URL = main;
                    if (hasAltUrl) queue.IN_ALT = alt;
                    queue._QueueMedia();
                }
                else tv._ChangeMediaWithAltTo(main, alt);
            }
        }

        [Obsolete] public void _ChangeAltMedia() => _ChangeMedia();

        public void _UseMainUrl()
        {
            if (tv.useAlternateUrl) { } else return;
            tv._UseMainUrl();
        }
        public void _UseAltUrl()
        {
            if (tv.useAlternateUrl) return;
            tv._UseQuestUrl();
        }


        // =============== TV EVENTS ===================

        // public void _TvMediaChange()
        // {
        //     var alternate = tv.useAlternateUrl;
        //     if (hasMain) mainIndicator.enabled = !alternate;
        //     if (hasAlt) altIndicator.enabled = alternate;
        // }

        public void _TvMediaStart()
        {
            duration = tv.videoDuration;
            startTime = tv.startTime;
            if (hasEndTime) endTime.text = getReadableTime(duration);
            if (hasSeek) { } else return;
            isLive = tv.isLive;
            if (isLive) seek.SetValueWithoutNotify(1f);
            else seek.SetValueWithoutNotify(0f);
            updateInfo();
        }

        public void _TvOwnerChange()
        {
            updateInfo();
        }

        // Once TV has loaded, update certain elements to correctly represent the TV state.
        public void _TvReady()
        {
            tvInit = true;
            if (hasMute)
            {
                if (tv.mute) _TvMute();
                else _TvUnMute();
            }
            if (hasAudioMode)
            {
                if (tv.audio3d) _TvAudioMode3d();
                else _TvAudioMode2d();
            }
            if (hasVideoPlayerSwap)
            {
                OUT_VIDEOPLAYER = tv.videoPlayer;
                _TvVideoPlayerChange();
            }
            if (hasVolume)
            {
                OUT_VOLUME = tv.volume;
                _TvVolumeChange();
            }
            if (hasMasterLock)
            {
                if (tv.locked) _TvLock();
                else _TvUnLock();
            }
            if (hasSync)
            {
                if (tv.syncToOwner) _TvSync();
                else _TvDeSync();
            }
            var state = tv.state;
            if (state == STOPPED) _TvStop();
            else
            {
                _TvMediaStart();
                if (state == PLAYING) _TvPlay();
                else if (state == PAUSED) _TvPause();
            }
            if (tv.loading) _TvLoading();
        }

        public void _TvPlay()
        {
            if (hasPlay) play.gameObject.SetActive(false);
            if (hasPause) pause.gameObject.SetActive(true);
            if (hasStop) stop.gameObject.SetActive(true);
            updateInfo();
        }


        public void _TvPause()
        {
            if (hasPlay) play.gameObject.SetActive(true);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(true);
        }

        public void _TvStop()
        {
            if (hasPlay) play.gameObject.SetActive(true);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(false);
            if (tv.errorOccurred) updateInfo();
        }

        public void _TvMute()
        {
            if (hasMute) muteIndicator.sprite = muted;
        }

        public void _TvUnMute()
        {
            if (hasMute) muteIndicator.sprite = unmuted;
        }

        public void _TvAudioMode3d()
        {
            if (hasAudioMode) audioModeIndicator.sprite = audio3d;
        }

        public void _TvAudioMode2d()
        {
            if (hasAudioMode) audioModeIndicator.sprite = audio2d;
        }

        public void _TvLoading()
        {
            if (hasPlay) play.gameObject.SetActive(false);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(true);
            if (hasLoadingBar)
            {
                loadingBar.gameObject.SetActive(true);
                loadingBar.value = 0f;
            }
            if (hasLoadingSpinner) loadingSpinnerContainer.SetActive(true);
            if (hasMainInput) mainUrlInput.gameObject.SetActive(false);
            if (hasAltInput) altUrlInput.gameObject.SetActive(false);
            isLoading = true;
        }
        public void _TvLoadingEnd()
        {
            if (hasPlay) play.gameObject.SetActive(true);
            if (hasPause) pause.gameObject.SetActive(true);
            if (hasLoadingBar)
            {
                loadingBar.gameObject.SetActive(false);
                loadingBar.value = 0f;
            }
            if (hasLoadingSpinner) loadingSpinnerContainer.SetActive(false);
            if (hasMainInput && !isLocked) mainUrlInput.gameObject.SetActive(true);
            if (hasAltInput && !isLocked) altUrlInput.gameObject.SetActive(true);
            isLoading = false;
        }
        public void _TvLoadingAbort()
        {
            _TvLoadingEnd();
        }
        public void _TvLock()
        {
            bool canControl = tv._IsPrivilegedUser();
            if (!canControl)
            {
                isLocked = true;
                if (hasMainInput) mainUrlInput.gameObject.SetActive(false);
                if (hasAltInput) altUrlInput.gameObject.SetActive(false);
            }
            if (hasMasterLock) masterLockIndicator.sprite = lockedIcon;
        }
        public void _TvUnLock()
        {
            isLocked = false;
            if (hasMainInput) mainUrlInput.gameObject.SetActive(true);
            if (hasAltInput) altUrlInput.gameObject.SetActive(true);
            if (hasMasterLock) masterLockIndicator.sprite = unlockedIcon;
        }
        public void _TvVolumeChange()
        {
            if (hasVolume) { } else return;
            if (volume.value != OUT_VOLUME)
            {
                skipVol = true;
                volume.value = OUT_VOLUME;
                if (volume.value == 0f) volumeIndicator.sprite = volumeOff;
                else if (volume.value == 1f) volumeIndicator.sprite = volumeHigh;
                else if (volume.value > 0.5f) volumeIndicator.sprite = volumeMed;
                else volumeIndicator.sprite = volumeLow;
            }
        }
        public void _TvVideoPlayerChange()
        {
            if (hasVideoPlayerSwap && videoPlayerSwap.value != OUT_VIDEOPLAYER)
                videoPlayerSwap.value = OUT_VIDEOPLAYER;
        }
        public void _TvVideoPlayerError()
        {
            if (hasPlay) play.gameObject.SetActive(false);
            if (hasPause) pause.gameObject.SetActive(false);
            if (hasStop) stop.gameObject.SetActive(true);
            if (hasInfo)
            {
                string t = "";
                var owner = Networking.GetOwner(tv.gameObject);
                if (showVideoOwner && VRC.SDKBase.Utilities.IsValid(owner))
                {
                    if (showVideoOwnerID)
                        t = $"[{owner.displayName} {owner.playerId}]";
                    else t = $"[{owner.displayName}]";
                }
                switch (OUT_ERROR)
                {
                    case VideoError.InvalidURL:
                        if (tv.isLive)
                            if (tv.retryCount > 0)
                                t += "(Stream Error) Stream not active. Rechecking...";
                            else t += "(Stream Error) Stream has ended or is unavailable.";
                        else t += "(Invalid URL) Could not resolve URL properly. Ensure there are no typos.";
                        break;
                    case VideoError.PlayerError:
                        if (tv.isLive)
                            if (tv.retryCount > 0)
                                t += "(Stream Error) Stream has stopped or failed. Rechecking...";
                            else t += "(Stream Error) Stream has ended.";
                        else t += "(Video Error) Unable to load video.";
                        break;
                    case VideoError.AccessDenied:
                        t += "(Access Denied) URL is blocked or 'Enable Untrusted URLs' is disabled.";
                        break;
                    case VideoError.RateLimited:
                        t += "(Rate Limited) Waiting 5 seconds to retry.";
                        break;
                    default:
                        t += $"(ERROR) {OUT_ERROR}";
                        break;
                }
                info.text = t;
            }
            if (hasLoadingBar && OUT_ERROR != VideoError.RateLimited)
            {
                loadingBar.gameObject.SetActive(false);
                loadingBar.value = 0f;
            }
            if (hasLoadingSpinner)
            {
                loadingSpinnerContainer.SetActive(false);
            }
        }

        public void _TvSync()
        {
            if (hasSync) syncModeIndicator.sprite = syncEnforced;
        }

        public void _TvDeSync()
        {
            if (hasSync) syncModeIndicator.sprite = localOnly;
        }

        // === helpers ===
        private bool isTvOwner() => Networking.IsOwner(localPlayer, tv.gameObject);
        private void updateInfo()
        {
            if (hasInfo)
            {
                var player = Networking.GetOwner(tv.gameObject);
                if (showVideoOwner && VRC.SDKBase.Utilities.IsValid(player))
                {
                    if (showVideoOwnerID)
                        info.text = $"[{player.displayName} {player.playerId}] {tv.localLabel}";
                    else info.text = $"[{player.displayName}] {tv.localLabel}";
                }
                else info.text = tv.localLabel;
            }
            if (hasSeek)
            {
                if (isTvOwner()) seek.enabled = true;
                else seek.enabled = false;
            }
            // if (hasMasterLock) masterLock.gameObject.SetActive(tv._IsPrivilegedUser());
            var alternate = tv.useAlternateUrl;
            if (hasMain) mainIndicator.enabled = !alternate;
            if (hasAlt) altIndicator.enabled = alternate;
        }

        private string getReadableTime(float time)
        {
            if (time == Mathf.Infinity) return "Live";
            if (float.IsNaN(time)) time = 0f;
            string early = time < 0 ? "-" : "";
            time = Mathf.Abs(time);
            int seconds = (int)time % 60;
            int minutes = (int)(time / 60) % 60;
            int hours = (int)(time / 60 / 60) % 60;
            if (hours > 0)
                return string.Format("{0}{1}:{2:D2}:{3:D2}", early, hours, minutes, seconds);
            return string.Format("{0}{1:D2}:{2:D2}", early, minutes, seconds);
        }

        private string getUrlDomain(VRCUrl url)
        {
            // strip the protocol
            var s = url.Get().Split(new string[] { "://" }, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return string.Empty;
            // strip everything after the first slash
            s = s[1].Split(new char[] { '/' }, 2, System.StringSplitOptions.None);
            // just to be sure, strip everything after the question mark if one is present
            s = s[0].Split(new char[] { '?' }, 2, System.StringSplitOptions.None);
            // return the url's domain value
            return s[0];
        }

        private string getUrlParam(VRCUrl url, string name)
        {
            // strip everything before the query parameters
            string[] s = url.Get().Split(new char[] { '?' }, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return string.Empty;
            // just to be sure, strip everything after the url bang if one is present
            s = s[1].Split(new char[] { '#' }, 2, System.StringSplitOptions.None);
            // attempt to find parameter name match
            s = s[0].Split('&');
            foreach (string param in s)
            {
                string[] p = param.Split(new char[] { '=' }, 2, System.StringSplitOptions.None);
                if (p[0] == name)
                {
                    return p[1];
                }
            }
            // if one can't be found, return an empty string
            return string.Empty;
        }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#fc7bcc>MediaControls ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#fc7bcc>MediaControls ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#fc7bcc>MediaControls ({debugLabel})</color>] {value}");
        }
    }
}
