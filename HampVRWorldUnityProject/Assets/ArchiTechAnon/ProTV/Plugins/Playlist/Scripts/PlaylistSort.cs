
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-1)]
    public class PlaylistSort : UdonSharpBehaviour
    {

        private const int SORT_DEFAULT = 0;
        private const int SORT_RANDOM = 1;
        private const int SORT_TITLEASC = 2;
        private const int SORT_TITLEDESC = 3;
        private const int SORT_TAGASC = 4;
        private const int SORT_TAGDESC = 5;

        [NonSerialized] public int IN_SORT_MODE = 0;
        [NonSerialized] public string IN_SORT_BY = "";

        public bool sortHiddenPlaylists = false;
        public Text sortInput;
        public Text sortDisplay;
        [Min(1), Tooltip("The number of milliseconds that the playlist search will occupy per frame. The higher the number the faster the playlist search goes, but the higher the frame lag is.")]
        public int sortAggressionLevel = 5;
        public Playlist[] playlistsToSort;

        // A same-length array corresponding to the rawView content, updated via the sorting algorithm. 
        // Can become out of sync if sorting is interrupted. Should reset to the rawView contents everytime a new sort is requested.
        // Once a sort is completed, the contents will be applied to the rawView and cascade the view updates.
        private int[] currentSortView;
        private string[] currentSortTitles;
        private string[] currentSortTags;



        private System.Diagnostics.Stopwatch sortDuration;
        private int[] stack;
        private int stackPtr;
        private bool currentSortActive = false;
        private double estimatedTotal;
        private Playlist currentSortPlaylist;
        private int currentSortIndex;
        private string currentSortTerm;
        private int currentSortMode;
        private bool sortingPartition = false;
        private int partitionStart;
        private int partitionEnd;
        private int partitionPivot;
        private int partitionIndex;
        private long itemTouches;
        private long unsortedItems;
        private double estimatedRemaining;


        private VRCPlayerApi local;
        private bool hasLocal = false;
        private bool hasSortDisplay = false;
        private bool hasSortInput = false;
        private bool inVR = false;
        private bool init = false;
        public bool debug = false;
        private string debugColor = "#ffaa66";
        private string debugLabel;

        private const string EMPTY = "";

        private void _Initialize()
        {
            if (init) return;
            init = true;
            debugLabel = gameObject.name;
            local = Networking.LocalPlayer;
            hasLocal = VRC.SDKBase.Utilities.IsValid(local);
            if (hasLocal) inVR = local.IsUserInVR();
            hasSortDisplay = sortDisplay != null;
            if (sortInput == null) sortInput = GetComponent<Text>();
            hasSortInput = sortInput != null;
        }

        void Start()
        {
            _Initialize();
        }

        // Input field event that sanitizes the input before calling the generic event.
        public void _UpdateSort()
        {
            if (!hasSortInput)
            {
                err("No sort input field provided. Unable to sort playlists.");
                return;
            }
            string sortBy = sortInput.text.Trim().ToLower();
            string s_mode = "0"; // Default to numerical sort order
            string sortTerm = "";
            int delimiterPos = sortBy.IndexOf(':');
            if (delimiterPos == -1) s_mode = sortBy;
            else
            {
                s_mode = sortBy.Substring(0, delimiterPos);
                sortTerm = sortBy.Substring(delimiterPos + 1);
            }
            int mode = 0;
            if (!int.TryParse(s_mode, out mode))
            {
                err("Unable to determine the sort mode. Please make sure the sort input starts with the desired sort mode. See docs for more information.");
                return;
            }
            IN_SORT_MODE = mode;
            IN_SORT_BY = sortTerm;
            _Sort();
        }

        // Generic event for allowing udon behaviours to trigger if desired.
        public void _Sort()
        {
            bool validationPassed = true;
            if (IN_SORT_MODE >= SORT_TAGDESC && IN_SORT_BY == "")
            {
                err("Cannot sort by tags if the sort term is not present. See docs for more information.");
                validationPassed = false;
            }
            if (IN_SORT_MODE < SORT_DEFAULT || SORT_TAGDESC < IN_SORT_MODE) // disallow invalid sort modes
            {
                err($"Invalid sort mode provided '{IN_SORT_MODE}'. See docs for more information.");
                validationPassed = false;
            }

            if (validationPassed)
            {
                log($"Sorting by {IN_SORT_MODE} with term '{IN_SORT_BY}'");
                startSort(IN_SORT_MODE, IN_SORT_BY);
            }
            IN_SORT_MODE = 0;
            IN_SORT_BY = "";
        }

        private bool playlistIsSortable(Playlist playlist) =>
            playlist != null && (sortHiddenPlaylists || playlist.gameObject.activeInHierarchy);

        private void startSort(int mode, string sortTerm)
        {

            if (playlistsToSort.Length == 0) return;
            // if (currentSortTerm == sortTerm && currentSortMode == mode) return; // no work to be done if search is same value
            currentSortIndex = -1;
            // safe max stack levels being 300 based on info from http://www.alienryderflex.com/quicksort/
            stack = new int[300];
            bool hasNextPlaylist = nextPlaylist();
            if (hasNextPlaylist)
            {
                // do the shortcut for "same as the raw list order"
                // TODO determine if this is even reasonable to do
                sortDuration = System.Diagnostics.Stopwatch.StartNew();
                if (mode == SORT_DEFAULT)
                {
                    foreach (Playlist playlist in playlistsToSort)
                        if (playlistIsSortable(playlist))
                            playlist._ResetSort();
                    currentSortActive = false;
                    if (hasSortDisplay)
                        sortDisplay.text = System.String.Format("Sort {0:P2} Complete ({1:F1}s)", 1f, sortDuration.Elapsed.TotalSeconds);
                    return;
                }
                if (mode == SORT_RANDOM)
                {
                    foreach (Playlist playlist in playlistsToSort)
                        if (playlistIsSortable(playlist))
                            playlist._Shuffle();
                    currentSortActive = false;
                    if (hasSortDisplay)
                        sortDisplay.text = System.String.Format("Sort {0:P2} Complete ({1:F1}s)", 1f, sortDuration.Elapsed.TotalSeconds);
                    return;
                }
                unsortedItems = 0;
                currentSortMode = mode;
                currentSortTerm = sortTerm;
                currentSortActive = true;

                // get the total entry count for all connected playlists
                foreach (Playlist playlist in playlistsToSort)
                    if (playlistIsSortable(playlist))
                        unsortedItems += playlist.urls.Length;

                SendCustomEventDelayedFrames(nameof(_ProcessSort), 1);
            }
        }

        private bool nextPlaylist()
        {
            currentSortIndex++;
            // log("Next Playlist pre-check: " + currentSortIndex);
            if (currentSortIndex >= playlistsToSort.Length) return false; // end of list
            currentSortPlaylist = playlistsToSort[currentSortIndex];
            while (!playlistIsSortable(currentSortPlaylist))
            {
                currentSortIndex++;
                if (currentSortIndex >= playlistsToSort.Length) return false; // end of list
                currentSortPlaylist = playlistsToSort[currentSortIndex];
            }
            currentSortPlaylist._ResetSortView();
            currentSortView = currentSortPlaylist.SortView;
            currentSortTitles = currentSortPlaylist.titles;
            currentSortTags = currentSortPlaylist.tags;


            // push initial partition start and end indices to the stack
            int end = currentSortView.Length - 1;
            stackPtr = 0;
            stack[stackPtr] = 0;
            stackPtr++;
            stack[stackPtr] = end;
            itemTouches = 0;
            unsortedItems = end;
            return true;
        }

        private void swap(int from, int to)
        {
            int temp = currentSortView[from];
            currentSortView[from] = currentSortView[to];
            currentSortView[to] = temp;
        }

        // get average of best and worst case value for the estimate [(O(NlogN) + O(N^2)) / 2]
        private double estimateRemainingItemTouches(long size)
        {
            double NlogN = Math.Log(size) * size;
            long N_2 = size * size;
            double avg = NlogN + N_2;
            avg = avg * 0.5;
            return Math.Floor(avg);
        }

        // understanding quicksort https://www.codesdope.com/course/algorithms-quicksort/
        public void _ProcessSort()
        {
            if (currentSortActive) { } else return;  // must trigger a proper sort to be allowed.
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            int len = currentSortPlaylist.urls.Length;

            while (timeout.ElapsedMilliseconds <= sortAggressionLevel)
            {
                // only run this logic when sorting mode is off
                if (sortingPartition) { }
                else
                {
                    // check for end of stack
                    if (stackPtr < 0)
                    {
                        currentSortPlaylist._UpdateSort();
                        long elapsed = sortDuration.ElapsedMilliseconds;
                        log($"QuickSort of {len} elements took {elapsed} ms");
                        log($"Progress: {itemTouches}/{estimatedTotal}");
                        if (!nextPlaylist())
                        {
                            currentSortActive = false;
                            estimatedRemaining = 0; // always make the sort complete state equal 100% at the end
                            log($"QuickSort of all attached playlists took {elapsed} ms");
                            break;
                        }
                    }

                    // grab the partition start and end values off the stack
                    partitionEnd = stack[stackPtr];
                    stackPtr--;
                    partitionStart = stack[stackPtr];
                    stackPtr--;

                    estimatedRemaining = estimateRemainingItemTouches(unsortedItems);
                    // remove the currently executing partition count from the unsorted tracker.
                    // any valid sub-level partitions will add their own size after the current partition is done.
                    int size = partitionEnd - partitionStart;
                    unsortedItems -= size;

                    // start sorting the next partition
                    sortingPartition = true;
                    partitionPivot = partitionStart;
                    partitionIndex = partitionStart;
                }
                // while sorting is enabled, have the timeout loop run the partition loop.
                if (sortingPartition)
                {
                    if (partitionIndex < partitionEnd)
                    {
                        // If the comparison determines that the swap should be in the left-side partition (less than)
                        // move the pivot's target index up one and swap the indexes so that the greater sized item is on the right
                        int compared = compare(partitionIndex, partitionEnd);
                        if (compared <= 0)
                        {
                            swap(partitionPivot, partitionIndex);
                            partitionPivot++;
                        }
                        // current item has been touched once comparison is made, regardless of its result
                        itemTouches++;
                    }
                    else
                    {
                        // Use the pivot target index to move the original pivot to it's new position between the partitions.
                        swap(partitionPivot, partitionEnd);
                        sortingPartition = false;
                    }
                    partitionIndex++;
                }

                // once the partitionin is complete, add the next partitions to the stack
                if (sortingPartition) { }
                else
                {

                    int nextPivot = partitionPivot - 1;
                    // add the left-side partition to the stack
                    if (nextPivot > partitionStart)
                    {
                        stackPtr++;
                        stack[stackPtr] = partitionStart;
                        stackPtr++;
                        stack[stackPtr] = nextPivot;
                        // add new sub-level partition size to the unsortedItems tracker
                        unsortedItems += (nextPivot - partitionStart);
                    }

                    nextPivot = partitionPivot + 1;
                    // add the right-side partition to the stack
                    if (nextPivot < partitionEnd)
                    {
                        stackPtr++;
                        stack[stackPtr] = nextPivot;
                        stackPtr++;
                        stack[stackPtr] = partitionEnd;
                        // add new sub-level partition size to the unsortedItems tracker
                        unsortedItems += (partitionEnd - nextPivot);
                    }
                }
            }
            // update the progress tracker
            estimatedTotal = itemTouches + estimatedRemaining;
            if (itemTouches > estimatedTotal) itemTouches = (long)estimatedTotal;
            if (hasSortDisplay)
                sortDisplay.text = System.String.Format("Sort {0:P2} Complete ({1:F1}s)", itemTouches / estimatedTotal, sortDuration.Elapsed.TotalSeconds);
            // trigger the next brokered update check.
            if (currentSortActive) SendCustomEventDelayedFrames(nameof(_ProcessSort), 1);
        }

        // compare should return FALSE if swapIndex should be in the left-side partition
        // and returns TRUE if swapIndex should be in the right-side partition
        private int compare(int swapIndex, int pivotIndex)
        {
            int mode = currentSortMode;
            string sortPrefix = currentSortTerm;
            string swapTitle = currentSortTitles[currentSortView[swapIndex]] ?? EMPTY;
            string pivotTitle = currentSortTitles[currentSortView[pivotIndex]] ?? EMPTY;
            swapTitle = swapTitle.Replace("\"", EMPTY);
            pivotTitle = pivotTitle.Replace("\"", EMPTY);

            int compared = String.Compare(swapTitle, pivotTitle);

            // titleASC compare
            if (mode == SORT_TITLEASC) return compared;
            // titleDESC compare
            if (mode == SORT_TITLEDESC) return compared * -1;
            // prefix only used for tags compare fallback to titleASC
            if (sortPrefix.Length == 0) return compared;

            sortPrefix = sortPrefix.ToLower() + ':';
            int prefixLength = sortPrefix.Length;
            string swapTagString = currentSortTags[currentSortView[swapIndex]] ?? EMPTY;
            string pivotTagString = currentSortTags[currentSortView[pivotIndex]] ?? EMPTY;
            bool swapHasPrefix = swapTagString.Contains(sortPrefix);
            bool pivotHasPrefix = pivotTagString.Contains(sortPrefix);
            if (pivotHasPrefix)
            {
                // if both have the prefix, defer to the tag comparisons
                if (swapHasPrefix) { }
                // if pivot has prefix but swap doesn't, swap will be after pivot
                else return 1;
            }
            else
            {
                // if pivot does not have the prefix when swap does
                if (swapHasPrefix) return -1;
                // no tag compares needed since neither has a prefix. sort by titleASC
                else return compared;
            }

            string[] pivotTags = pivotTagString.Split(',');
            string[] swapTags = swapTagString.Split(',');

            foreach (string swapTag in swapTags)
            {
                if (swapTag.StartsWith(sortPrefix))
                {
                    foreach (string pivotTag in pivotTags)
                    {
                        if (pivotTag.StartsWith(sortPrefix))
                        {
                            string pTag = pivotTag.Substring(prefixLength);
                            string sTag = swapTag.Substring(prefixLength);
                            int tagCompared = String.Compare(sTag, pTag);
                            // if the sort value is the same, defer to swapping by titleASC instead
                            if (tagCompared != 0)
                            {
                                // ASC tag value comparison, 
                                if (mode == SORT_TAGASC) return tagCompared;
                                // DESC tag value comparison
                                if (mode == SORT_TAGDESC) return tagCompared * -1;
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            // in all other conditions, sort by titleASC
            return compared;
        }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistSort)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistSort)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistSort)} ({debugLabel})</color>] {value}");
        }
    }
}
