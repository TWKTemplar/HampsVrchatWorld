
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class Queue : UdonSharpBehaviour
    {
        private const string TSTR_QUEUE_LIMIT_REACHED = "Queue Limit Reached";
        private const string TSTR_PLAYER_LIMIT_REACHED = "Personal Limit Reached";

        [NonSerialized] public VRCUrl IN_URL = new VRCUrl("");
        [NonSerialized] public VRCUrl IN_ALT = new VRCUrl("");
        [NonSerialized] public string IN_TITLE = string.Empty;

        public TVManagerV2 tv;
        public RectTransform listContainer;
        // public GameObject template;
        [Obsolete] public VRCUrlInputField urlInput { get => mainUrlInput; set => mainUrlInput = value; }
        public VRCUrlInputField mainUrlInput;
        [Obsolete] public VRCUrlInputField altUrlInput { get => questUrlInput; set => mainUrlInput = value; }
        public VRCUrlInputField questUrlInput;
        public Button queueMedia;
        public Button nextMedia;
        public InputField titleInput;
        public Text toasterMsg;
        public byte maxVideosPerPlayer = 2;
        public bool preventDuplicateVideos = true;
        public bool showUrlsInQueue = true;
        // Hardcoded for now. Will be configurable in a future update.
        [NonSerialized] public const int maxQueuedVideos = 20;
        private Text[] urlDisplays = new Text[maxQueuedVideos];
        private Text[] titleDisplays = new Text[maxQueuedVideos];
        private Text[] ownerDisplays = new Text[maxQueuedVideos];

        private int OUT_OWNER;
        private VideoError OUT_ERROR;
        private Slider loadingBar;
        private float loadingBarDamp;
        private bool isLoading = false;

        [UdonSynced] private VRCUrl[] urls = new VRCUrl[maxQueuedVideos];
        [UdonSynced] private VRCUrl[] alts = new VRCUrl[maxQueuedVideos];
        [UdonSynced] private string[] titles = new string[maxQueuedVideos];
        [UdonSynced] private int[] owners = new int[maxQueuedVideos];
        private Button[] removal = new Button[maxQueuedVideos];

        private int queueLength;
        private bool hasLoadingBar;
        private bool hasUrlInput;
        private bool hasAltInput;
        private bool hasTitleInput;
        private bool hasToaster;
        private bool hasQueueMedia;
        private bool hasNextMedia;
        private bool isOwner { get => Networking.IsOwner(gameObject); }
        private bool requestedByMe = false;
        private const string EMPTY = "";
        private bool noActiveMedia = true;
        private VRCPlayerApi localPlayer;
        private bool hasLocalPlayer;
        private bool init = false;
        private bool debug = true;
        private string debugLabel;
        private string debugColor = "yellow";

        public void _Initialize()
        {
            if (init) return;
            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            if (tv == null)
            {
                debugLabel = $"<Missing TV Ref>/{name}";
                err("The TV reference was not provided. Please make sure the queue knows what TV to connect to.");
                return;
            }
            debugLabel = $"{tv.gameObject.name}/{name}";
            debug = tv.debug;
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                var t = listContainer.GetChild(i);
                if (t == null) break;
                var ownerT = t.Find("Owner");
                var titleT = t.Find("Title");
                var urlT = t.Find("Url");
                var removeT = t.Find("Remove");
                if (urlT != null) urlDisplays[i] = urlT.GetComponent<Text>();
                if (titleT != null) titleDisplays[i] = titleT.GetComponent<Text>();
                if (ownerT != null) ownerDisplays[i] = ownerT.GetComponent<Text>();
                if (removeT != null)
                {
                    removal[i] = removeT.GetComponent<Button>();
                    removeT.gameObject.SetActive(false);
                }
                titles[i] = EMPTY;
                urls[i] = VRCUrl.Empty;
            }
            loadingBar = listContainer.GetChild(0).GetComponentInChildren<Slider>(true);
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = localPlayer != null;
            hasLoadingBar = loadingBar != null;
            hasUrlInput = mainUrlInput != null;
            hasAltInput = questUrlInput != null;
            hasTitleInput = titleInput != null;
            hasToaster = toasterMsg != null;
            hasQueueMedia = queueMedia != null;
            hasNextMedia = nextMedia != null;
            _UpdateUrlInput();
            updateUI();
            // this plugin's priority should be higher than most other plugins
            tv._RegisterUdonSharpEventReceiverWithPriority(this, 100);
            init = true;
        }

        void Start()
        {
            _Initialize();
        }

        void LateUpdate() => _InternalUpdate();

        public void _InternalUpdate()
        {
            if (isLoading) if (hasLoadingBar)
                {
                    if (loadingBar.value > 0.95f) return;
                    if (loadingBar.value > 0.8f)
                        loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, 0.4f);
                    else
                        loadingBar.value = Mathf.SmoothDamp(loadingBar.value, 1f, ref loadingBarDamp, 0.3f);
                }
        }

        new void OnPostSerialization(SerializationResult result)
        {
            if (result.success) { }
            else
            {
                RequestSerialization();
            }
        }

        new void OnDeserialization()
        {
            updateUI();
        }

        new void OnPlayerLeft(VRCPlayerApi p)
        {
            if (isTVOwner())
            {
                var pid = localPlayer.playerId;
                var oldpid = p.playerId;
                for (int i = 0; i < owners.Length; i++)
                {
                    if (oldpid == owners[i])
                    {
                        owners[i] = pid;
                    }
                }
                updateUI();
            }
        }

        // new void OnOwnershipTransferred(VRCPlayerApi p)
        // {
        //     isOwner = p.isLocal;
        // }


        // === UI Events ===
        public void _UpdateUrlInput()
        {
            if (hasQueueMedia)
            {
                if (!hasUrlInput || mainUrlInput.GetUrl().Get() == string.Empty)
                    queueMedia.gameObject.SetActive(false);
                else queueMedia.gameObject.SetActive(true);
            }
        }

        public void _QueueMediaInput()
        {
            if (hasUrlInput)
            {
                IN_URL = mainUrlInput.GetUrl();
                mainUrlInput.SetUrl(VRCUrl.Empty);
            }
            if (IN_URL.Get() == "") return; // no url present
            if (hasAltInput)
            {
                IN_ALT = questUrlInput.GetUrl();
                questUrlInput.SetUrl(VRCUrl.Empty);
            }
            if (hasTitleInput)
            {
                IN_TITLE = titleInput.text ?? EMPTY;
                titleInput.text = EMPTY;
            }
            _QueueMedia();
        }

        public void _QueueMedia()
        {
            _Initialize();
            string inUrl = IN_URL.Get();
            if (inUrl == EMPTY) return; // no url present
            int target = -1;
            for (int i = maxQueuedVideos - 1; i >= 0; i--)
            {
                var url = urls[i];
                url = url ?? VRCUrl.Empty;
                if (url.Get() != EMPTY) break; // find the first entry that is empty
                target = i;
            }
            bool validationFailed = false;
            bool unprivileged = !tv._IsPrivilegedUser();
            if (target == -1)
            {
                warn("Queue is full. Wait until another media has been cleared.");
                validationFailed = true;
            }
            else if (unprivileged && tv.locked)
            {
                warn("TV is locked. You must be a privileged user to queue media while TV is locked.");
                validationFailed = true;
            }
            else if (unprivileged && personalVideosQueued() >= maxVideosPerPlayer)
            {
                warn("Personal queue limit reached. Either remove one or wait for the next one to play.");
                validationFailed = true;
            }
            else if (preventDuplicateVideos && videoIsQueued(inUrl))
            {
                warn("Media is already in queue. Duplicate media are not allowed.");
                validationFailed = true;
            }
            if (validationFailed)
            {
                // purge input data and immediately return
                IN_URL = VRCUrl.Empty;
                IN_ALT = VRCUrl.Empty;
                IN_TITLE = EMPTY;
                return;
            }
            // log($"QueueMedia: Assigning new media to entry {target}");
            if (!isOwner) Networking.SetOwner(localPlayer, gameObject);
            urls[target] = IN_URL;
            alts[target] = IN_ALT;
            if (!showUrlsInQueue && IN_TITLE == EMPTY)
            {
                // if URLS are hidden, but no title is available, use the url as the title.
                IN_TITLE = inUrl;
            }
            titles[target] = IN_TITLE;
            owners[target] = localPlayer.playerId;
            if (hasLoadingBar && target == 0)
                loadingBar.value = urls[0].Get() == tv.url.Get() ? 1f : 0f;
            collapseEntries();
            if (noActiveMedia) play();
            IN_URL = VRCUrl.Empty;
            IN_ALT = VRCUrl.Empty;
            IN_TITLE = EMPTY;
        }

        public void _Remove()
        {
            _Initialize();
            var index = getInteractedIndex();
            if (index > -1 && (localPlayer.playerId == owners[index] || tv._IsPrivilegedUser()))
            {
                if (!isOwner) Networking.SetOwner(localPlayer, gameObject);
                if (index == 0 && _MatchCurrentUrl(true))
                {
                    log("Removing active queue item.");
                    tv._Stop();
                    requestNext();
                }
                else
                {
                    log($"Removing queue item {index}");
                    urls[index] = VRCUrl.Empty;
                    collapseEntries();
                }
            }
        }

        public void _Purge()
        {
            _Initialize();
            bool isPrivileged = tv._IsPrivilegedUser();
            if (isPrivileged) log("Privileged user purging entire queue.");
            int purgeCount = 0;
            for (int i = 0; i < queueLength; i++)
            {
                if (isPrivileged || localPlayer.playerId == owners[i])
                {
                    urls[i] = VRCUrl.Empty;
                    purgeCount++;
                    if (i == 0) tv._Stop();
                }
            }
            if (purgeCount > 0)
            {
                collapseEntries();
                log($"Purged {purgeCount} queue entries");
            }

        }

        public void _Next()
        {
            _Initialize();
            requestNext();
        }


        // ======== NETWORK METHODS ===========

        private void requestNext()
        {
            if (!tv.locked || isTVOwner())
            {
                requestedByMe = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_RequestNext));
            }
        }

        public void ALL_RequestNext() // PUT ALL Next BUTTON CHECKS HERE
        {
            if (tv.loading || !hasLocalPlayer) return; // ignore any requests for next while loading to prevent next spamming
            if (tv.locked && isTVOwner())
            { // only allow the TV owner to act if the tv is locked
                if (requestedByMe)
                { // only allow self-requested NEXT calls when the TV is locked.
                    if (_MatchCurrentUrl(true))
                    { // if the current url is in the TV, switch to the next URL
                        if (!isOwner) Networking.SetOwner(localPlayer, gameObject);
                        nextURL();
                    }
                    play();
                }
            }
            else
            {
                if (_MatchCurrentUrl(true))
                { // if the current url is in the TV
                    if (isOwner && _CheckNextUrl(false) || localPlayer.playerId == owners[1])
                    {
                        // allow pass through for queue owner if there isn't another media in queue
                        //      This allows for media end to clear the last url from the queue
                        // update owner of queue to the owner of next queued media otherwise
                        if (!isOwner) Networking.SetOwner(localPlayer, gameObject);
                        nextURL();
                        play();
                    }
                }
                else
                {
                    if (_CheckCurrentUrl(true) && localPlayer.playerId == owners[0])
                    {
                        // if there is a URL in the queue, make the owner of that queue entry play the video
                        play();
                    }
                }
            }
            requestedByMe = false;
        }


        // === TV Events ===

        public void _TvMediaChange()
        {
            // if the tv media changes and the current URL does not match, collapse the entries
            if (isOwner && _MatchCurrentUrl(false))
                collapseEntries();
        }

        public void _TvMediaEnd()
        {
            noActiveMedia = true;
            if (isTVOwner())
            {
                if (_MatchCurrentUrl(true)) // urls[0] matches the tv url
                    requestNext(); // attempt queueing the next media
                else play(); // attempt to play urls[0]
            }
        }

        public void _TvMediaStart()
        {
            // track active media state and update the TV label if the current media has a title associated with it
            noActiveMedia = false;
            if (titles[0] != EMPTY)
                tv.localLabel = titles[0];
        }

        public void _TvMediaLoop()
        {
            // enforce active media state when media loops
            noActiveMedia = false;
        }

        public void _TvVideoPlayerError()
        {
            if (tv.errorOccurred) { } else return; // only proceed if tv signal an error actually occurred
            isLoading = false;
            noActiveMedia = true;
        }

        public void _TvLoading()
        {
            // ONLY enable loading if the current url matches
            if (hasLoadingBar) loadingBar.value = 0f;
            if (_MatchCurrentUrl(true))
                isLoading = true;
        }

        public void _TvLoadingEnd()
        {
            if (isLoading)
            {
                isLoading = false;
                if (hasLoadingBar) loadingBar.value = 1f;
            }
        }

        public void _TvLoadingAbort()
        {
            isLoading = false;
            if (hasLoadingBar) loadingBar.value = 0f;
        }

        public void _TvLock()
        {
            bool privileged = tv._IsPrivilegedUser();
            if (hasUrlInput) mainUrlInput.gameObject.SetActive(privileged);
            if (hasAltInput) questUrlInput.gameObject.SetActive(privileged);
            if (hasTitleInput) titleInput.gameObject.SetActive(privileged);
            if (hasNextMedia) nextMedia.gameObject.SetActive(privileged);
        }

        public void _TvUnLock()
        {
            if (hasUrlInput) mainUrlInput.gameObject.SetActive(true);
            if (hasAltInput) questUrlInput.gameObject.SetActive(true);
            if (hasTitleInput) titleInput.gameObject.SetActive(true);
            if (hasNextMedia) nextMedia.gameObject.SetActive(true);
        }

        // ======== HELPER METHODS ============


        private void playNext()
        {
            if (_MatchCurrentUrl(true)) nextURL();
            play();
        }

        private void nextURL()
        {
            if (isOwner)
            {
                urls[0] = VRCUrl.Empty;
                if (hasLoadingBar) loadingBar.value = 0f;
                collapseEntries();
            }
        }

        private void play()
        {
            if (urls[0].Get() != EMPTY)
            {
                noActiveMedia = false;
                log($"Next URL - {urls[0]} | title '{titles[0]}'");
                tv._ChangeMediaWithAltTo(urls[0], alts[0]);
            }
        }

        private bool urlMatch(VRCUrl check, bool shouldMatch)
        {
            check = check ?? VRCUrl.Empty;
            bool matches = tv.url.Get() == check.Get();
            return matches == shouldMatch;
        }

        private bool urlExist(VRCUrl check, bool shouldExist)
        {
            check = check ?? VRCUrl.Empty;
            bool exists = check != VRCUrl.Empty;
            return exists == shouldExist;
        }


        public bool _MatchCurrentUrl(bool shouldMatch) => urlMatch(urls[0], shouldMatch);
        public bool _CheckCurrentUrl(bool shouldExist) => urlExist(urls[0], shouldExist);
        public bool _CheckNextUrl(bool shouldExist) => urlExist(urls[1], shouldExist);

        private void collapseEntries()
        {
            var _urls = new VRCUrl[maxQueuedVideos];
            var _alts = new VRCUrl[maxQueuedVideos];
            var _titles = new string[maxQueuedVideos];
            var _owners = new int[maxQueuedVideos];
            int index = 0;
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                var url = urls[i];
                url = url ?? VRCUrl.Empty;
                _urls[index] = url;
                if (url.Get() == EMPTY)
                {
                    alts[i] = VRCUrl.Empty;
                    titles[i] = EMPTY;
                    owners[i] = -1;
                    continue;
                }
                _alts[index] = alts[i];
                _titles[index] = titles[i];
                _owners[index] = owners[i];
                index++;
            }
            queueLength = index;
            log($"Updated to {index} entries");
            for (; index < maxQueuedVideos; index++)
            {
                // fill the remainder of the arrays with default values
                _urls[index] = VRCUrl.Empty;
                _alts[index] = VRCUrl.Empty;
                _titles[index] = EMPTY;
                _owners[index] = -1;
            }
            urls = _urls;
            alts = _alts;
            titles = _titles;
            owners = _owners;
            RequestSerialization();
            updateUI();
        }

        private void updateUI()
        {
            if (hasToaster)
                toasterMsg.text = EMPTY;
            var controlBypass = tv._IsPrivilegedUser();
            int count = 0;
            int personalCount = 0;
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                var url = urls[i];
                var title = titles[i];
                url = url ?? VRCUrl.Empty;
                if (url.Get() == EMPTY)
                {
                    listContainer.GetChild(i).gameObject.SetActive(false);
                    continue;
                }
                var owner = VRCPlayerApi.GetPlayerById(owners[i]);
                if (!VRC.SDKBase.Utilities.IsValid(owner)) return; // invalid player
                if (ownerDisplays[i] != null)
                {
                    if (debug)
                        ownerDisplays[i].text = $"{owner.displayName} [{owner.playerId}]";
                    else
                        ownerDisplays[i].text = owner.displayName;
                }
                bool hasTitle = title != EMPTY;
                if (titleDisplays[i] != null)
                    if (hasTitle) titleDisplays[i].text = title;
                    else titleDisplays[i].text = url.Get();

                if (urlDisplays[i] != null)
                    if (showUrlsInQueue) urlDisplays[i].text = url.Get();
                    else urlDisplays[i].text = EMPTY;

                var remove = removal[i];
                if (remove != null)
                {
                    var isOwner = localPlayer == owner;
                    remove.gameObject.SetActive(isOwner || controlBypass);
                    if (isOwner) personalCount++;
                }
                listContainer.GetChild(i).gameObject.SetActive(true);
                count++;
            }
            if (hasLoadingBar && urls[0].Get() != tv.url.Get())
                loadingBar.value = 0f;

            if (count >= maxQueuedVideos)
            {
                if (hasUrlInput)
                {
                    mainUrlInput.SetUrl(VRCUrl.Empty);
                    mainUrlInput.gameObject.SetActive(false);
                }
                if (hasTitleInput) titleInput.gameObject.SetActive(false);
                if (hasToaster) toasterMsg.text = TSTR_QUEUE_LIMIT_REACHED;
            }
            else if (!controlBypass && personalCount >= maxVideosPerPlayer)
            {
                if (hasUrlInput)
                {
                    mainUrlInput.SetUrl(VRCUrl.Empty);
                    mainUrlInput.gameObject.SetActive(false);
                }
                if (hasTitleInput) titleInput.gameObject.SetActive(false);
                if (hasToaster) toasterMsg.text = TSTR_PLAYER_LIMIT_REACHED;
            }
            else
            {
                if (hasUrlInput) mainUrlInput.gameObject.SetActive(true);
                if (hasTitleInput) titleInput.gameObject.SetActive(true);
                if (hasToaster) toasterMsg.text = "";
            }
        }

        private int getInteractedIndex()
        {
            for (int i = 0; i < maxQueuedVideos; i++)
            {
                if (removal[i].interactable) { }
                else return i;
            }
            return -1;
        }

        private int personalVideosQueued()
        {
            int count = 0;
            for (int i = 0; i < maxQueuedVideos; i++)
                if (localPlayer.playerId == owners[i])
                    count++;
            return count;
        }

        private bool videoIsQueued(string url)
        {
            foreach (VRCUrl queued in urls)
                if (queued != null && queued.Get() == url)
                    return true;
            return false;
        }

        private bool isTVOwner() => Networking.IsOwner(localPlayer, tv.gameObject);

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Queue)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Queue)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Queue)} ({debugLabel})</color>] {value}");
        }
    }
}
