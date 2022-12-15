using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class Playlist : UdonSharpBehaviour
    {

        [HideInInspector] public ScrollRect scrollView;
        [HideInInspector] public RectTransform content;
        [HideInInspector] public GameObject template;
        [HideInInspector] public TVManagerV2 tv;
        [HideInInspector] public Queue queue;
        [HideInInspector] public bool shuffleOnLoad = false;
        [HideInInspector] public bool autoplayList = false;
        [HideInInspector] public bool prioritizeOnInteract = true;
        [HideInInspector] public bool autoplayOnLoad = true;
        [HideInInspector] public bool loopPlaylist = true;
        [HideInInspector] public bool startFromRandomEntry = false;
        [HideInInspector] public bool continueWhereLeftOff = true;
        [HideInInspector] public bool autoplayOnVideoError = true;
        [HideInInspector] public bool showUrls;
        [HideInInspector] public VRCUrl[] urls;
        [HideInInspector] public VRCUrl[] alts;
        [HideInInspector] public string[] titles;
        [HideInInspector] public string[] tags;
        [HideInInspector] public Sprite[] images;
        [HideInInspector] public PlaylistData storage;
        [NonSerialized] public int viewOffset;
        [NonSerialized] public int SWITCH_TO_INDEX = -1;
        // entry caches
        private RectTransform[] entryCache;
        private Button[] buttonCache;
        private Text[] urlCache;
        private Text[] titleCache;
        private Image[] imageCache;
        // A 1 to 1 array corresponding to each original entry specifying whether the element should be filtered (aka hidden) in the views.
        private bool[] hidden;
        // an array of the same size as the urls that stores corresponding references to the indexes within the urls array.
        // the order of this array is what gets modified when the playlist gets sorted.
        private int[] sortView = new int[0];
        // an array of a variable size (length of sortView or less) that represents the list of url indexes that are visible for rendering in the current view
        // this array also contains values which correspond to the indexes of the URL list, which may be non-sequential based on the sortView order
        private int[] filteredView = new int[0];
        // an array that represents the visible render shown in the scene based on the filteredView array
        // unlike the previous two, this array's contents corresponds to indexes of the filteredView array
        // eg: to get the actual URL based on a particular entry of the current view, you'd access it via urls[filteredView[currentView[index]]]
        private int[] currentView = new int[0];
        private int nextSortViewIndex = 0;
        private int currentSortViewIndex = -1;
        private VideoError OUT_ERROR;
        private bool isLoading = false;
        private bool updateTVLabel = false;
        private Slider loading;
        private float loadingBarDamp;
        private float loadingPercent;
        private Canvas[] canvases;
        private bool hasLoading;
        private bool hasNoTV;
        private bool hasQueue;
        private bool skipScrollbar;
        private string debugLabel;
        [NonSerialized] public bool init = false;
        private bool debug = true;
        private string debugColor = "#ff8811";
        [HideInInspector] public TextAsset _EDITOR_importSrc;
        [HideInInspector] public bool _EDITOR_manualToImport;
        [HideInInspector] public bool _EDITOR_autofillAltURL;
        [HideInInspector] public string _EDITOR_autofillFormat = "$URL";

        // Getter Helpers
        public bool[] Hidden { get => hidden; }
        public int[] SortView { get => sortView; }
        public int[] FilteredView { get => filteredView; }
        public int CurrentEntryIndex { get => currentSortViewIndex == -1 ? -1 : sortView[currentSortViewIndex]; }
        public int NextEntryIndex { get => currentSortViewIndex == -1 ? -1 : sortView[nextSortViewIndex]; }
        public VRCUrl CurrentEntryMainUrl { get => currentSortViewIndex == -1 ? VRCUrl.Empty : urls[sortView[currentSortViewIndex]]; }
        public VRCUrl CurrentEntryAltUrl { get => currentSortViewIndex == -1 ? VRCUrl.Empty : alts[sortView[currentSortViewIndex]]; }
        public string CurrentEntryTags { get => currentSortViewIndex == -1 ? string.Empty : tags[sortView[currentSortViewIndex]]; }
        public string CurrentEntryInfo { get => currentSortViewIndex == -1 ? string.Empty : titles[sortView[currentSortViewIndex]]; }
        public Sprite CurrentEntryImage { get => currentSortViewIndex == -1 ? null : images[sortView[currentSortViewIndex]]; }



        public void _Initialize()
        {
            if (init) return;
            template.SetActive(false);
            if (storage != null)
            {
                urls = storage.urls;
                alts = storage.alts;
                titles = storage.titles;
                tags = storage.tags;
                images = storage.images;
            }
            cacheEntryRefs();
            hidden = new bool[urls.Length];
            sortView = new int[urls.Length];
            _ResetSortView();
            if (shuffleOnLoad) shuffle(sortView, 3);
            cacheFilteredView();
            if (tv == null) tv = transform.GetComponentInParent<TVManagerV2>();
            hasNoTV = tv == null;
            hasQueue = queue != null;


            if (titles.Length != urls.Length)
            {
                warn($"Titles count ({titles.Length}) doesn't match Urls count ({urls.Length}).");
            }
            if (hasNoTV)
            {
                debugLabel = $"<Missing TV Ref>/{name}";
                err("The TV reference was not provided. Please make sure the playlist knows what TV to connect to.");
            }
            else if (urls.Length == 0)
            {
                debugLabel = $"{tv.gameObject.name}/{name}";
                warn("No entries in the playlist.");
            }
            else
            {
                debug = tv.debug;
                if (autoplayList)
                {
                    nextSortViewIndex = currentSortViewIndex = 0;
                    if (startFromRandomEntry)
                        nextSortViewIndex = currentSortViewIndex = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * (sortView.Length - 1));
                    if (autoplayOnLoad && Networking.LocalPlayer.isMaster)
                    {
                        int nextIndex = sortView[nextSortViewIndex];
                        if (hasQueue)
                        {
                            // if something else has not yet prepared an entry for the queue, go ahead
                            var inurl = queue.IN_URL;
                            if (inurl == null || string.IsNullOrWhiteSpace(inurl.Get()))
                            {
                                queue.IN_URL = urls[nextIndex];
                                queue.IN_ALT = alts[nextIndex];
                                queue.IN_TITLE = titles[nextIndex];
                                queue.SendCustomEventDelayedSeconds(nameof(Queue._QueueMedia), 3f);
                                pickNext();
                            }
                        }
                        else
                        {
                            // if something else has not yet assigned the autoplay, go ahead
                            if (tv.autoplayURL == null || string.IsNullOrWhiteSpace(tv.autoplayURL.Get()))
                            {
                                tv.autoplayURL = urls[nextIndex];
                                tv.autoplayURLAlt = alts[nextIndex];
                                pickNext();
                            }
                        }
                    }
                }
                tv._RegisterUdonSharpEventReceiverWithPriority(this, 120);
                debugLabel = $"{tv.gameObject.name}/{name}";
                init = true;
                _SeekView(sortViewIndexToFilteredViewIndex(nextSortViewIndex));
            }
            init = true;
            canvases = GetComponentsInChildren<Canvas>();
        }

        void Start()
        {
            _Initialize();
        }

        void OnEnable()
        {
            _Initialize();
            foreach (Canvas c in canvases) c.enabled = true;
        }

        void OnDisable()
        {
            _Initialize();
            foreach (Canvas c in canvases) c.enabled = false;
        }

        void LateUpdate() => _InternalUpdate();

        public void _InternalUpdate()
        {
            if (isLoading)
            {
                if (hasLoading) loadingPercent = loading.value;
                if (loadingPercent > 0.95f) return;
                if (loadingPercent > 0.8f)
                    loadingPercent = Mathf.SmoothDamp(loadingPercent, 1f, ref loadingBarDamp, 0.4f);
                else loadingPercent = Mathf.SmoothDamp(loadingPercent, 1f, ref loadingBarDamp, 0.3f);
                if (hasLoading) loading.value = loadingPercent;
            }
        }


        // === TV EVENTS ===

        public void _TvMediaStart()
        {
            if (hasNoTV) return;
            // only handle updating the TV label once the video has been loaded successfully to avoid losing the title from the previous video
            if (updateTVLabel) { } else currentSortViewIndex = findSortViewIndex();
            if (currentSortViewIndex > -1)
            {
                string title = titles[sortView[currentSortViewIndex]];
                if (title != string.Empty)
                    tv.localLabel = title;
                else if (showUrls) { }
                else tv.localLabel = "--Playlist Video--";
            }
            updateTVLabel = false;
        }

        public void _TvMediaEnd()
        {
            if (hasNoTV) return;
            if (autoplayList && !tv.loading && isTVOwner())
                if (loopPlaylist || nextSortViewIndex != 0)
                    if (!hasQueue || queue._CheckNextUrl(false))
                        _SwitchTo(nextSortViewIndex);
        }

        public void _TvMediaChange()
        {
            if (hasNoTV) return;
            // log("Media Change");
            if (autoplayList && !continueWhereLeftOff)
            {
                if (tv.urlMain.Get() != urls[sortView[wrap(nextSortViewIndex - 1)]].Get())
                    nextSortViewIndex = 0;
            }
            retargetActive();
        }

        public void _TvVideoPlayerError()
        {
            if (hasNoTV) return;
            if (autoplayList) { } else return;
            if (tv.errorOccurred) { } else return; // only proceed if the tv signals that an error has actually occurred.
            if (tv.urlMain.Get() == urls[sortView[wrap(nextSortViewIndex - 1)]].Get())
            {
                if (!loopPlaylist || nextSortViewIndex != 0)
                {
                    int rawIndex = sortView[nextSortViewIndex];
                    currentSortViewIndex = nextSortViewIndex;
                    updateTVLabel = true;
                    log($"Error detected. Switching to entry {rawIndex}: {urls[rawIndex]}");
                    tv._DelayedChangeMediaWithAltTo(urls[rawIndex], alts[rawIndex]);
                    pickNext();
                }
            }
        }

        public void _TvLoading()
        {
            isLoading = true;
            loadingPercent = 0f;
            if (hasLoading) loading.value = 0f;
        }

        public void _TvLoadingEnd()
        {
            isLoading = false;
            loadingPercent = 1f;
            if (hasLoading) loading.value = 1f;
        }

        public void _TvLoadingAbort()
        {
            isLoading = false;
            loadingPercent = 0f;
            if (hasLoading) loading.value = 0f;
        }


        // === UI EVENTS ===

        public void _Next()
        {
            nextSortViewIndex = wrap(nextSortViewIndex + 1);
            _SwitchTo(nextSortViewIndex);
        }

        public void _Previous()
        {
            nextSortViewIndex = wrap(nextSortViewIndex - 2);
            _SwitchTo(nextSortViewIndex);
        }

        public void _SwitchToDetected()
        {
            if (!init) return;
            for (int i = 0; i < buttonCache.Length; i++)
            {
                if (!buttonCache[i].interactable)
                {
                    int sortViewIndex = Array.IndexOf(sortView, currentViewIndexToRawIndex(i));
                    log($"Detected view index {i}. Switching to list index {sortViewIndex}.");
                    _SwitchTo(sortViewIndex);
                    return;
                }
            }
        }

        public void _UpdateView()
        {
            if (!init || skipScrollbar) return;
            log("Update View");
            int filteredViewIndex = 0;
            if (scrollView.verticalScrollbar != null)
                filteredViewIndex = Mathf.FloorToInt((1f - scrollView.verticalScrollbar.value) * filteredView.Length);
            seekView(filteredViewIndex);
            retargetActive();
        }

        public void _Shuffle()
        {
            log("Randomizing sort");
            shuffle(sortView, 3);
            cacheFilteredView(); // must recache the filtered view after a shuffle to update to the new sortView order
            _SeekView(0);
        }

        public void _ResetSort()
        {
            log("Resetting sort to default");
            _ResetSortView();
            cacheFilteredView(); // must recache the filtered view after a shuffle to update to the new sortView order
            _SeekView(0);
        }

        public void _AutoPlay()
        {
            if (autoplayList) return; // already autoplay, skip
            autoplayList = true;
            if (startFromRandomEntry)
                nextSortViewIndex = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * sortView.Length - 1);
            else pickNext();
            if (tv.stateSync != 1 && !tv.loading)
            {
                _SwitchTo(nextSortViewIndex);
            }
        }

        public void _ManualPlay()
        {
            autoplayList = false;
        }

        public void _ToggleAutoPlay()
        {
            if (autoplayList) _ManualPlay();
            else _AutoPlay();
        }

        public void _ChangeAutoPlayTo(bool active)
        {
            if (active) _AutoPlay();
            else _ManualPlay();
        }

        // === Public Helper Methods

        public void _Switch()
        {
            if (SWITCH_TO_INDEX > -1)
            {
                _SwitchTo(SWITCH_TO_INDEX);
                SWITCH_TO_INDEX = -1;
            }
        }

        public void _SwitchTo(int sortViewIndex)
        {
            if (hasNoTV) return; // wait until the current video loading finishes/fails
            if (!hasQueue) if (isLoading) return; // if not using a queue, wait until the tv finishes loading before playing another video
            if (sortViewIndex >= sortView.Length)
                err($"Playlist Item {sortViewIndex} doesn't exist.");
            else if (sortViewIndex == -1) { } // do nothing
            else
            {
                nextSortViewIndex = currentSortViewIndex = sortViewIndex;
                log($"Switching to playlist item {sortViewIndex}");
                int index = sortView[nextSortViewIndex];
                if (prioritizeOnInteract)
                    tv._SetUdonSharpSubscriberPriorityToHigh(this);
                if (hasQueue)
                {
                    queue.IN_URL = urls[index];
                    queue.IN_ALT = alts[index];
                    string title = titles[index];
                    if (title != string.Empty) queue.IN_TITLE = title;
                    queue._QueueMedia();
                }
                else
                {
                    if (tv.tvInit) tv._ChangeMediaWithAltTo(urls[index], alts[index]);
                    else tv._DelayedChangeMediaWithAltTo(urls[index], alts[index]);
                    updateTVLabel = true;
                }
                pickNext();
            }
        }

        public void _SeekView(int filteredViewIndex)
        {
            if (!init || filteredViewIndex == -1) return;
            // log("Seek View");
            filteredViewIndex = Mathf.Clamp(filteredViewIndex, 0, filteredView.Length - 1);
            if (scrollView.verticalScrollbar != null)
            {
                skipScrollbar = true;
                scrollView.verticalScrollbar.value = 1 - ((float)filteredViewIndex) / filteredView.Length;
                skipScrollbar = false;
            }
            seekView(filteredViewIndex);
            retargetActive();
        }

        public void _UpdateFilter(bool[] hide)
        {
            if (hide.Length != urls.Length)
            {
                log("Filter array must be the same size as the list of urls in the playlist");
                return;
            }
            hidden = hide;
            cacheFilteredView();
            // since the filtered array changed size, recalculate the total entries height
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            // limit offset to the url max minus the last "page", account for the "extra" overflow row as well.
            var maxRow = (filteredView.Length - 1) / horizontalCount + 1;
            var contentHeight = maxRow * item.height;

            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            _SeekView(0);
        }

        public void _UpdateSort()
        {
            cacheFilteredView();
            _SeekView(0);
        }

        // === Helper Methods ===

        private void shuffle(int[] view, int cycles)
        {
            for (int j = 0; j < cycles; j++)
                VRC.SDKBase.Utilities.ShuffleArray(view);
        }

        // prepare the sortView with the default index mapping
        public void _ResetSortView()
        {
            for (int i = 0; i < sortView.Length; i++) sortView[i] = i;
        }

        // TODO change the logic so that filteredView has a persistent array that only modifies the internal values
        // instead of creating a new array every time. 
        // It's currently wasting memory when filters are applied in fast sequence (like searching on every letter change)
        // It should loop through, apply visible index values in order, then set to -1 the remainder of values
        // MUST also cache the number of visible entries as a field variable for later use.
        // That way it doesn't need to loop through the array every time to figure out how many visible there are.
        private void cacheFilteredView()
        {
            var count = 0;
            // determine how many non-hidden items there are
            for (int i = 0; i < sortView.Length; i++)
                if (!hidden[sortView[i]]) count++;
            var cache = new int[count];
            count = 0;
            // compose new array with the non-hidden entry indexs
            for (int i = 0; i < sortView.Length; i++)
            {
                var index = sortView[i];
                if (!hidden[index]) cache[count++] = index;
            }
            filteredView = cache;
        }

        private void cacheEntryRefs()
        {
            int cacheSize = content.childCount;
            entryCache = new RectTransform[cacheSize];
            buttonCache = new Button[cacheSize];
            urlCache = new Text[cacheSize];
            titleCache = new Text[cacheSize];
            imageCache = new Image[cacheSize];
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform entry = (RectTransform)content.GetChild(i);
                entryCache[i] = entry;

                buttonCache[i] = entry.GetComponentInChildren<Button>();

                var url = entry.Find("Url");
                urlCache[i] = url == null ? null : url.GetComponent<Text>();

                var title = entry.Find("Title");
                titleCache[i] = title == null ? null : title.GetComponent<Text>();

                var image = entry.Find("Image");
                imageCache[i] = image == null ? null : image.GetComponent<Image>();
            }
        }

        public void seekView(int filteredViewIndex)
        {
            // modifies the scope of the view, cache the offset for later use
            viewOffset = calculateFilteredViewOffset(filteredViewIndex);
            updateCurrentView(viewOffset);
        }

        // Takes in the current index and calculates a rounded value based on the horizontal count
        // This ensures that elements don't incidentally shift horizontally and only vertically while scrolling
        private int calculateFilteredViewOffset(int filteredViewIndex)
        {
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "row", account for the "extra" overflow row as well.
            var maxRawRow = filteredView.Length / horizontalCount + 1;
            // clamp the min/max row to the view area boundries
            var maxRow = Mathf.Min(maxRawRow, maxRawRow - verticalCount);
            if (maxRow == 0) maxRow = 1;

            var maxOffset = maxRow * horizontalCount;
            var currentRow = filteredViewIndex / horizontalCount; // int DIV causes stepped values, good
            var currentOffset = currentRow * horizontalCount;
            // currentOffset will be smaller than maxOffset when the scroll limit has not yet been reached
            var targetOffset = Mathf.Min(currentOffset, maxOffset);
            // log($"Raw {rawOffset} | H/V Count {horizontalCount}/{verticalCount} | Max RawRow/Row/Offset {maxRawRow}/{maxRow}/{maxOffset} | Current Row/Offset {currentRow}/{currentOffset}");
            return Mathf.Max(0, targetOffset);
        }

        private void updateCurrentView(int filteredViewIndex)
        {
            currentView = new int[content.childCount];
            int numOfUrls = filteredView.Length;
            // string _log = "None";
            for (int i = 0; i < content.childCount; i++)
            {
                if (filteredViewIndex >= numOfUrls)
                {
                    // urls have exceeded count, hide the remaining entries
                    content.GetChild(i).gameObject.SetActive(false);
                    currentView[i] = -1;
                    continue;
                }
                // if (i == 0) _log = $"{filteredView[filteredViewIndex]}";
                // else _log += $", {filteredView[filteredViewIndex]}";
                var entry = content.GetChild(i);
                entry.gameObject.SetActive(true);
                // update entry contents
                var index = filteredView[filteredViewIndex];
                var url = urlCache[i];
                if (showUrls && url != null) url.text = urls[index].Get();
                var title = titleCache[i];
                if (title != null) title.text = titles[index];
                var image = imageCache[i];
                if (image != null)
                {
                    var imageEntry = images[index];
                    image.sprite = imageEntry;
                    image.gameObject.SetActive(imageEntry != null);
                }
                currentView[i] = filteredViewIndex;
                filteredViewIndex++;
            }
            // log(_log);
        }

        private void retargetActive()
        {
            // if autoplay is disabled, try to see if the current media matches one on the playlist, if so, indicate loading
            if (hasLoading) loading.value = 0f;
            int found = findCurrentViewIndex();
            // cache the found index's Slider component, otherwise null
            if (found > -1)
            {
                loading = content.GetChild(found).GetComponentInChildren<Slider>();
                hasLoading = loading != null;
                if (hasLoading) loading.value = loadingPercent;
            }
            else
            {
                // log($"Media index not within view");
                loading = null;
                hasLoading = false;
            }
        }

        private int findCurrentViewIndex()
        {
            if (hasNoTV) return -1;
            var url = tv.urlMain.Get();
            // if the current index is playing on the TV and not hidden, 
            //  return either it's position in the current view, or -1 if it's not visible in the current view
            if (currentSortViewIndex > -1)
            {
                var rawIndex = sortView[currentSortViewIndex];
                if (urls[rawIndex].Get() == url)
                    if (!hidden[rawIndex])
                        return Array.IndexOf(currentView, Array.IndexOf(filteredView, rawIndex));
            }

            // then if the current index IS hidden or IS NOT playing on the TV, 
            // attempt a fuzzy search to find another index that matches that URL
            // do not need to check for hidden here as current view already has that taken into account
            for (int i = 0; i < currentView.Length; i++)
            {
                var listIndex = currentViewIndexToRawIndex(i);
                if (listIndex > -1 && urls[listIndex].Get() == url)
                {
                    // log($"List index {listIndex} matches TV url at view index {i}");
                    return i;
                }
            }
            // log("No matches at all");
            return -1;
        }

        private int findSortViewIndex()
        {
            var len = sortView.Length;
            var url = tv.urlMain.Get();
            for (int i = 0; i < len; i++)
            {
                var rawIndex = sortView[i];
                if (urls[rawIndex].Get() == url)
                    return i;
            }
            return -1;
        }

        // take a given index (typically derived from the button a player clicks on in the UI)
        // and reverse the values through the arrays to get the original list index.
        private int currentViewIndexToRawIndex(int index)
        {
            if (index == -1) return -1;
            if (index >= currentView.Length) return -1;
            int filteredViewIndex = currentView[index];
            if (filteredViewIndex == -1) return -1;
            if (filteredViewIndex >= filteredView.Length) return -1;
            return filteredView[filteredViewIndex];
        }

        private int filteredViewIndexToSortViewIndex(int filteredViewIndex)
        {
            if (filteredViewIndex == -1) return -1;
            return Array.IndexOf(sortView, filteredView[filteredViewIndex]);
        }

        private int sortViewIndexToFilteredViewIndex(int sortViewIndex)
        {
            if (sortViewIndex == -1) return -1;
            return Array.IndexOf(filteredView, sortView[sortViewIndex]);
        }


        private void pickNext()
        {
            var nextPossibleIndex = nextSortViewIndex;
            do
            {
                if (nextPossibleIndex != nextSortViewIndex)
                    log($"Item {nextPossibleIndex} is missing, skipping");
                nextPossibleIndex = wrap(nextPossibleIndex + 1);
                if (nextSortViewIndex == nextPossibleIndex) break; // exit if the entire list has been traversed
            } while (urls[sortView[nextPossibleIndex]].Get() == VRCUrl.Empty.Get());
            log($"Next playlist item {nextPossibleIndex}");
            nextSortViewIndex = nextPossibleIndex;
        }

        private int wrap(int value)
        {
            if (value < 0) value = sortView.Length + value; // adds a negative
            else if (value >= sortView.Length) value = value - sortView.Length; // subtracts the full length
            return value;
        }

        private bool isTVOwner() => !hasNoTV && Networking.IsOwner(tv.gameObject);

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Playlist)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Playlist)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Playlist)} ({debugLabel})</color>] {value}");
        }
    }
}
