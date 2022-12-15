
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistPaginator : UdonSharpBehaviour
    {
        public Playlist playlist;
        private Scrollbar scrollbar;
        private RectTransform content;
        private int perRow;
        private int perColumn;
        private int totalItems;

        private VRCPlayerApi local;
        private bool init = false;
        public bool debug = false;
        private string debugColor = "#ffaa66";
        private string debugLabel;

        private void initialize()
        {
            if (init) return;
            if (playlist == null) {
                log("Must specify playlist");
                return;
            }
            scrollbar = playlist.scrollView.verticalScrollbar;
            content = playlist.content;
            Rect max = playlist.scrollView.viewport.rect;
            Rect item = ((RectTransform)playlist.template.transform).rect;
            perRow = Mathf.FloorToInt(max.width / item.width);
            if (perRow == 0) perRow = 1;
            perColumn = content.childCount / perRow;

            debugLabel = playlist.gameObject.name;
            local = Networking.LocalPlayer;
            init = true;
        }

        void Start()
        {
            initialize();
        }

        public void _PrevPage() {
            seekView(perRow * perColumn * -1 + 1);
        }

        public void _PrevRow() {
            seekView(-perRow);
        }

        public void _NextRow() {
            seekView(perRow);
        }

        public void _NextPage() {
            seekView(perRow * perColumn - 1);
        }

        private void seekView(int shift) {

            playlist._SeekView(playlist.viewOffset + shift);
        }


        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistPaginator)} ({debugLabel})</color>] {value}");
        }
        private void warn(string value)
        {
            Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistPaginator)} ({debugLabel})</color>] {value}");
        }
        private void err(string value)
        {
            Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistPaginator)} ({debugLabel})</color>] {value}");
        }
    }
}
