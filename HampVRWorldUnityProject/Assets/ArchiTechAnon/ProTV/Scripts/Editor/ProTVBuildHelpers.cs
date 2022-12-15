
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.SDKBase.Editor.BuildPipeline;

namespace ArchiTech.Editor
{
    public class ProTVBuildHelpers : IVRCSDKBuildRequestedCallback
    {

        public int callbackOrder { get { return -2; } }

        public bool OnBuildRequested(VRCSDKRequestedBuildType type)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            Scene scene = EditorSceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            List<TVManagerV2> tvsList = new List<TVManagerV2>();
            List<Playlist> playlistsList = new List<Playlist>();
            List<Controls_ActiveState> controlsList = new List<Controls_ActiveState>();
            foreach (GameObject root in roots)
            {
                // grab all necessary script references
                tvsList.AddRange(root.GetComponentsInChildren<TVManagerV2>(true));
                controlsList.AddRange(root.GetComponentsInChildren<Controls_ActiveState>(true));
                playlistsList.AddRange(root.GetComponentsInChildren<Playlist>(true));
            }
            TVManagerV2[] tvs = tvsList.ToArray();
            Controls_ActiveState[] controls = controlsList.ToArray();
            Playlist[] playlists = playlistsList.ToArray();

            UpdateVersions(scene, roots);
            UpdateDropdowns(scene, controls);
            UpdateAutoplayOffsets(scene, tvs, playlists);
            AutoUpgrade(scene, controls);
            UnityEngine.Debug.LogFormat($"ProTV Build Helpers execution time: {sw.ElapsedMilliseconds}ms");
            return true;
        }

        public static void UpdateVersions(Scene scene, GameObject[] roots)
        {
            string[] possibleVersions = AssetDatabase.FindAssets("VERSION t:TextAsset");
            string actualVersion = "Assets/ArchiTechAnon/ProTV/VERSION.md";
            foreach (string checkAsset in possibleVersions)
            {
                string checkVersion = AssetDatabase.GUIDToAssetPath(checkAsset);
                if (checkVersion.Contains("ProTV/VERSION")) actualVersion = checkVersion;
            }
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(actualVersion);
            if (textAsset == null)
            {
                UnityEngine.Debug.LogWarning("The ProTV VERSION file could not be found in the Assets. Unable to inject the version number into the scene.");
                return;
            }
            string versionNumber = textAsset.text.Trim();
            foreach (GameObject root in roots)
            {
                Text[] possibles = root.GetComponentsInChildren<Text>(true);
                foreach (Text possible in possibles)
                {
                    string possibleName = possible.gameObject.name.ToLower();
                    if (possibleName.Contains("protv") && possibleName.Contains("version") && possible.text != versionNumber)
                    {
                        possible.text = versionNumber;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(possible);
                        EditorUtility.SetDirty(possible);
                    }
                }
            }
        }

        public static void UpdateDropdowns(Scene scene, Controls_ActiveState[] controls)
        {
            foreach (Controls_ActiveState _control in controls)
            {
                Controls_ActiveState control = _control;
#if !UDONSHARP // U# 0.x support
                control = control.GetUdonSharpComponent<Controls_ActiveState>();
#endif
                if (control.videoPlayerSwap == null) continue;
                TVManagerV2 tv = control.tv;
                if (tv == null) continue;
#if !UDONSHARP // U# 0.x support
                tv = tv.GetUdonSharpComponent<TVManagerV2>();
#endif
                if (tv.videoManagers == null) continue;
                control.videoPlayerSwap.ClearOptions();
                List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
                foreach (VideoManagerV2 _manager in tv.videoManagers)
                {
                    VideoManagerV2 manager = _manager;
                    if (manager == null)
                    {
                        options.Add(new Dropdown.OptionData("<Missing Ref>"));
                        continue;
                    }
#if !UDONSHARP // U# 0.x support
                    manager = manager.GetUdonSharpComponent<VideoManagerV2>();
#endif
                    if (manager.customLabel != null && manager.customLabel != string.Empty)
                        options.Add(new Dropdown.OptionData(manager.customLabel));
                    else options.Add(new Dropdown.OptionData(manager.gameObject.name));
                }
                control.videoPlayerSwap.AddOptions(options);
                PrefabUtility.RecordPrefabInstancePropertyModifications(control.videoPlayerSwap);
                EditorUtility.SetDirty(control.videoPlayerSwap);
            }
        }

        public static void UpdateAutoplayOffsets(Scene scene, TVManagerV2[] tvs, Playlist[] playlists)
        {
            var count = 0;
            for (int i = 0; i < tvs.Length; i++)
            {
                TVManagerV2 tv = tvs[i];
#if !UDONSHARP // U# 0.x support
                tv = tv.GetUdonSharpComponent<TVManagerV2>();
#endif
                if (!tv.gameObject.activeInHierarchy) continue; // skip tvs that are not enabled by default
                bool hasAutoplay = !string.IsNullOrWhiteSpace(tv.autoplayURL.Get()) || !string.IsNullOrWhiteSpace(tv.autoplayURLAlt.Get());
                if (!hasAutoplay)
                {
                    foreach (Playlist _playlist in playlists)
                    {
                        Playlist playlist = _playlist;
#if !UDONSHARP // U# 0.x support
                        playlist = playlist.GetUdonSharpComponent<Playlist>();
#endif
                        if (playlist.autoplayOnLoad && playlist.tv != null)
                        {
                            var _tv = playlist.tv;
#if !UDONSHARP // U# 0.x support
                            _tv = _tv.GetUdonSharpComponent<TVManagerV2>();
#endif
                            if (_tv == tv) hasAutoplay = true;
                        }
                    }
                }
                if (hasAutoplay)
                {
                    tv.autoplayStartOffset = 5f * count;
                    // Debug.Log($"{tv.transform.GetHierarchyPath()} gets autoplay start offset {5f * count}");
                    count++;
                }
                else tv.autoplayStartOffset = 0f;

#if !UDONSHARP // U# 0.x support
                tv.ApplyProxyModifications();
                PrefabUtility.RecordPrefabInstancePropertyModifications(UdonSharpEditorUtility.GetBackingUdonBehaviour(tv));
#endif
                PrefabUtility.RecordPrefabInstancePropertyModifications(tv);
                EditorUtility.SetDirty(tv);
            }
        }

        public static void AutoUpgrade(Scene scene, Controls_ActiveState[] controls)
        {
            foreach (Controls_ActiveState _control in controls)
            {
                Controls_ActiveState control = _control;
#if !UDONSHARP // U# 0.x support
                control = control.GetUdonSharpComponent<Controls_ActiveState>();
#endif
                if (control.updateMainUrl != null)
                {
                    if (control.activateUrls == null)
                        control.activateUrls = control.updateMainUrl;
                    control.updateMainUrl = null;
#if !UDONSHARP // U# 0.x support
                    control.ApplyProxyModifications();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(UdonSharpEditorUtility.GetBackingUdonBehaviour(control));
#endif
                    PrefabUtility.RecordPrefabInstancePropertyModifications(control);
                    EditorUtility.SetDirty(control);
                }

            }
        }

    }
}