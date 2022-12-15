using System;
using System.IO;
using System.Text;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRC.SDKBase;

namespace ArchiTech.Editor
{
    [CustomEditor(typeof(Playlist))]
    public class PlaylistEditor : UnityEditor.Editor
    {
        private static string newEntryIndicator = "@";
        private static string entryAltIndicator = "^";
        private static string entryImageIndicator = "/";
        private static string entryTagIndicator = "#";
        Playlist script;
        TVManagerV2 tv;
        ScrollRect scrollView;
        RectTransform content;
        GameObject template;
        Queue queue;
        bool shuffleOnLoad;
        bool autoplayList;
        bool autoplayOnLoad;
        bool loopPlaylist;
        bool prioritizeOnInteract;
        bool startFromRandomEntry;
        bool continueWhereLeftOff;
        bool showUrls = true;
        bool autofillAltURL;
        string autofillFormat;
        VRCUrl[] urls;
        VRCUrl[] alts;
        string[] titles;
        string[] tags;
        Sprite[] images;
        int visibleCount;
        Vector2 scrollPos;
        ChangeAction updateMode = ChangeAction.NOOP;
        bool manualToImport = false;
        TextAsset importSrc;
        int perPage = 10;
        int currentFocus;
        int lastFocus;
        int entriesCount;
        int imagesCount;
        int targetEntry;
        bool sanitize = true;

        VRCUrl[] currentPageUrls;
        VRCUrl[] currentPageAlts;
        string[] currentPageTitles;
        string[] currentPageTags;
        Sprite[] currentPageImages;
        int currentPageStart;
        int currentPageEnd;
        bool recachePage = true;

        int rawEntryCount;


        private enum ChangeAction
        {
            NOOP, OTHER,
            MOVEUP, MOVEDOWN,
            ADD, REMOVE, REMOVEALL,
            UPDATESELF, UPDATEALL, UPDATEVIEW
        }

        public override void OnInspectorGUI()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            LoadData();
            EditorGUI.BeginChangeCheck();
            RenderChangeCheck();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Modify Playlist Content");
                SaveData();
            }
            EditorGUILayout.LabelField($"Inspector frametime: {stopwatch.ElapsedMilliseconds}ms");
        }

        private void LoadData()
        {
            script = (Playlist)target;
            if (script.storage == null) script.storage = script.gameObject.GetComponentInChildren<PlaylistData>();
            if (sanitize)
            {
                sanitize = false;
                if (script.storage != null)
                {
                    urls = script.storage.urls;
                    alts = script.storage.alts;
                    titles = script.storage.titles;
                    tags = script.storage.tags;
                    images = script.storage.images;
                }
                else
                {
                    urls = script.urls;
                    alts = script.alts;
                    titles = script.titles;
                    tags = script.tags;
                    images = script.images;
                }
                urls = (VRCUrl[])NormalizeArray(urls, typeof(VRCUrl), 0);
                rawEntryCount = urls.Length;
                alts = (VRCUrl[])NormalizeArray(alts, typeof(VRCUrl), rawEntryCount);
                titles = (string[])NormalizeArray(titles, typeof(string), rawEntryCount);
                tags = (string[])NormalizeArray(tags, typeof(string), rawEntryCount);
                images = (Sprite[])NormalizeArray(images, typeof(Sprite), rawEntryCount);
            }
        }

        private void RenderNoChangeCheck() { }

        private void RenderChangeCheck()
        {
            showProperties();
            showListControls();
            showListEntries();
        }

        private void SaveData()
        {
            if (updateMode == ChangeAction.NOOP) return; // don't save the data if current op was none
            if (updateMode != ChangeAction.OTHER)
            {
                updateScene();
                sanitize = true;
                recachePage = true;
            }
            updateMode = ChangeAction.NOOP;

            script.tv = tv;
            script.scrollView = scrollView;
            script.content = content;
            script.template = template;
            script.queue = queue;
            script.showUrls = showUrls;
            script.shuffleOnLoad = shuffleOnLoad;
            script.autoplayList = autoplayList;
            script.autoplayOnLoad = autoplayOnLoad;
            script.loopPlaylist = loopPlaylist;
            script.startFromRandomEntry = startFromRandomEntry;
            script.continueWhereLeftOff = continueWhereLeftOff;
            script.prioritizeOnInteract = prioritizeOnInteract;
            if (script.storage != null)
            {
                script.storage.urls = urls;
                script.storage.alts = alts;
                script.storage.titles = titles;
                script.storage.tags = tags;
                script.storage.images = images;

                script.urls = null;
                script.alts = null;
                script.titles = null;
                script.tags = null;
                script.images = null;
            }
            else
            {
                script.urls = urls;
                script.alts = alts;
                script.titles = titles;
                script.tags = tags;
                script.images = images;
            }
            script._EDITOR_importSrc = importSrc;
            script._EDITOR_manualToImport = manualToImport;
            script._EDITOR_autofillAltURL = autofillAltURL;
            script._EDITOR_autofillFormat = autofillFormat;
        }



        private void showProperties()
        {
            EditorGUILayout.Space();
            tv = (TVManagerV2)EditorGUILayout.ObjectField("TV", script.tv, typeof(TVManagerV2), true);
            if (tv != script.tv) updateMode = ChangeAction.OTHER;
            queue = (Queue)EditorGUILayout.ObjectField("Queue", script.queue, typeof(Queue), true);
            if (queue != script.queue) updateMode = ChangeAction.OTHER;
            scrollView = (ScrollRect)EditorGUILayout.ObjectField("Playlist ScrollView", script.scrollView, typeof(ScrollRect), true);
            if (scrollView != script.scrollView) updateMode = ChangeAction.OTHER;
            content = (RectTransform)EditorGUILayout.ObjectField("Playlist Item Container", script.content, typeof(RectTransform), true);
            if (content != script.content) updateMode = ChangeAction.OTHER;
            template = (GameObject)EditorGUILayout.ObjectField("Playlist Item Template", script.template, typeof(GameObject), true);
            if (template != script.template) updateMode = ChangeAction.OTHER;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Shuffle Playlist on Load");
                shuffleOnLoad = EditorGUILayout.Toggle(script.shuffleOnLoad);
                if (shuffleOnLoad != script.shuffleOnLoad) updateMode = ChangeAction.OTHER;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Autoplay?");
                autoplayList = EditorGUILayout.Toggle(script.autoplayList);
                if (autoplayList != script.autoplayList) updateMode = ChangeAction.OTHER;
            }

            if (autoplayList)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PrefixLabel("Autoplay on Load");
                    autoplayOnLoad = EditorGUILayout.Toggle(script.autoplayOnLoad);
                    if (autoplayOnLoad != script.autoplayOnLoad) updateMode = ChangeAction.OTHER;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PrefixLabel("Start From Random Entry");
                    startFromRandomEntry = EditorGUILayout.Toggle(script.startFromRandomEntry);
                    if (startFromRandomEntry != script.startFromRandomEntry) updateMode = ChangeAction.OTHER;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PrefixLabel("Loop Playlist");
                    loopPlaylist = EditorGUILayout.Toggle(script.loopPlaylist);
                    if (loopPlaylist != script.loopPlaylist) updateMode = ChangeAction.OTHER;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PrefixLabel("Continue From Last Known Entry");
                    continueWhereLeftOff = EditorGUILayout.Toggle(script.continueWhereLeftOff);
                    if (continueWhereLeftOff != script.continueWhereLeftOff) updateMode = ChangeAction.OTHER;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PrefixLabel("Prioritize Playlist on Interact");
                    prioritizeOnInteract = EditorGUILayout.Toggle(script.prioritizeOnInteract);
                    if (prioritizeOnInteract != script.prioritizeOnInteract) updateMode = ChangeAction.OTHER;
                }
            }
            else
            {
                continueWhereLeftOff = script.continueWhereLeftOff;
                prioritizeOnInteract = script.prioritizeOnInteract;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Show Urls in Playlist?");
                showUrls = EditorGUILayout.Toggle(script.showUrls);
                if (showUrls != script.showUrls) updateMode = ChangeAction.OTHER;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Autofill Quest Urls");
                autofillAltURL = EditorGUILayout.Toggle(script._EDITOR_autofillAltURL);
                if (autofillAltURL != script._EDITOR_autofillAltURL) {
                    updateMode = ChangeAction.OTHER;
                    if (!autofillAltURL) autofillFormat = "$URL"; // reset to default
                }
            }
            if (autofillAltURL)
            {
                EditorGUILayout.HelpBox("Put $URL (uppercase is important) wherever you want the main url to be inserted. Eg: https://mydomain.tld/?url=$URL", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Autofill Format");
                    autofillFormat = EditorGUILayout.TextField(script._EDITOR_autofillFormat);
                    if (autofillFormat != script._EDITOR_autofillFormat) updateMode = ChangeAction.OTHER;
                }
            }
        }

        private void showListControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(); // 1
            EditorGUILayout.BeginVertical(); // 2
            EditorGUILayout.LabelField("Video Playlist Items", GUILayout.Width(120f), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Update Scene", GUILayout.MaxWidth(100f)))
            {
                updateMode = ChangeAction.UPDATEALL;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(manualToImport);
                if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
                {
                    // get where to save the file
                    string defaultName = script._EDITOR_importSrc != null ? script._EDITOR_importSrc.name : "CustomPlaylist";
                    string destination = EditorUtility.SaveFilePanelInProject("Playlist Export", defaultName, "txt", "Save the playlist in your assets folder");
                    if (!string.IsNullOrWhiteSpace(destination))
                    {
                        Debug.Log($"Saving playlist to file {destination}");
                        // write the playlist content
                        File.WriteAllText(destination, pickle(), Encoding.UTF8);
                        AssetDatabase.Refresh();
                        // load the new playlist file into the import mode
                        TextAsset t = AssetDatabase.LoadAssetAtPath<TextAsset>(destination);
                        script._EDITOR_manualToImport = true;
                        script._EDITOR_importSrc = t;
                        updateMode = ChangeAction.OTHER;
                    }
                }
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Copy", GUILayout.ExpandWidth(false)))
                {
                    GUIUtility.systemCopyBuffer = pickle();
                }
            }
            EditorGUILayout.EndVertical(); // end 2
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(); // 2
            string detection = "";
            if (importSrc != null && script._EDITOR_manualToImport)
                detection = $" | Detected: {entriesCount} urls with {imagesCount} images";
            manualToImport = EditorGUILayout.ToggleLeft($"Load From Text File{detection}", script._EDITOR_manualToImport);
            if (manualToImport != script._EDITOR_manualToImport) updateMode = ChangeAction.OTHER;
            if (manualToImport)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    importSrc = (TextAsset)EditorGUILayout.ObjectField(script._EDITOR_importSrc, typeof(TextAsset), false, GUILayout.MaxWidth(300f));
                    if (importSrc != script._EDITOR_importSrc)
                    {
                        updateMode = ChangeAction.OTHER;
                        entriesCount = 0;
                        imagesCount = 0;
                    }
                    if (importSrc != null)
                    {
                        if (entriesCount == 0) entriesCount = countEntries(importSrc.text);
                        if (imagesCount == 0) imagesCount = countImages(importSrc.text);

                        if (GUILayout.Button("Import", GUILayout.ExpandWidth(false)))
                        {
                            parseContent(importSrc.text);
                            updateMode = ChangeAction.UPDATEALL;
                        }
                    }
                }
            }
            else
            {
                importSrc = script._EDITOR_importSrc;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Entry", GUILayout.MaxWidth(100f)))
                    {
                        updateMode = ChangeAction.ADD;
                    }

                    EditorGUI.BeginDisabledGroup(rawEntryCount == 0);
                    if (GUILayout.Button("Remove All", GUILayout.MaxWidth(100f)))
                    {
                        updateMode = ChangeAction.REMOVEALL;
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }

            var urlCount = rawEntryCount;
            var currentPage = currentFocus / perPage;
            var maxPage = urlCount / perPage;
            var oldFocus = currentFocus;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(currentPage == 0);
                if (GUILayout.Button("<<")) currentFocus -= perPage;
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(currentFocus == 0);
                if (GUILayout.Button("<")) currentFocus -= 1;
                EditorGUI.EndDisabledGroup();
                currentFocus = EditorGUILayout.IntSlider(currentFocus, 0, urlCount - 1, GUILayout.ExpandWidth(true));
                if (currentFocus != lastFocus)
                {
                    recachePage = true;
                    lastFocus = currentFocus;
                }
                GUILayout.Label($"/ {urlCount}");

                EditorGUI.BeginDisabledGroup(currentFocus == urlCount);
                if (GUILayout.Button(">")) currentFocus += 1;
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(currentPage == maxPage);
                if (GUILayout.Button(">>")) currentFocus += perPage;
                EditorGUI.EndDisabledGroup();
            }

            if (oldFocus != currentFocus)
            {
                updateMode = ChangeAction.UPDATEVIEW;
            }

            EditorGUILayout.EndVertical(); // end 2
            EditorGUILayout.EndHorizontal(); // end 1
        }

        private int countEntries(string text)
        {
            if (text.Trim().Length == 0) return 0;
            string[] lines = text.Trim().Split('\n');
            int count = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith(newEntryIndicator)) count++;
            }
            return count;
        }

        private int countImages(string text)
        {
            if (text.Trim().Length == 0) return 0;
            string[] lines = text.Trim().Split('\n');
            int count = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith(entryImageIndicator)) count++;
            }
            return count;
        }

        private void parseContent(string text)
        {
            text = text.Trim();
            string[] lines = text.Split('\n');
            int count = countEntries(text);
            urls = new VRCUrl[count];
            alts = new VRCUrl[count];
            titles = new string[count];
            tags = new string[count];
            images = new Sprite[count];
            count = -1;
            bool foundAlt = false;
            bool foundTagString = false;
            bool foundImage = false;
            bool foundTitle = false;
            VRCUrl currentAlt = VRCUrl.Empty;
            string currentTitle = "";
            uint missingTitles = 0;
            foreach (string l in lines)
            {
                foundTitle = currentTitle.Length > 0;
                var line = l.Trim();
                if (line.StartsWith(newEntryIndicator))
                {
                    if (count > -1)
                    {
                        if (currentTitle.Length == 0)
                        {
                            Debug.Log($"Missing title at index {count}");
                            missingTitles++;
                        }
                        titles[count] = currentTitle.Trim();
                        currentTitle = "";
                        foundAlt = false;
                        foundTagString = false;
                        foundImage = false;
                        foundTitle = false;
                    }
                    count++;
                    urls[count] = new VRCUrl(line.Substring(newEntryIndicator.Length).Trim());
                    if (autofillAltURL) alts[count] = new VRCUrl(autofillFormat.Replace("$URL", urls[count].Get()));
                    continue;
                }
                if (count == -1) continue;
                if (!foundTitle && !foundAlt && line.StartsWith(entryAltIndicator))
                {
                    foundAlt = true;
                    alts[count] = new VRCUrl(line.Substring(entryAltIndicator.Length).Trim());
                    continue;
                }
                if (!foundTitle && !foundImage && line.StartsWith(entryImageIndicator))
                {
                    foundImage = true;
                    string assetFile = line.Substring(entryImageIndicator.Length).Trim();
                    images[count] = (Sprite)AssetDatabase.LoadAssetAtPath(assetFile, typeof(Sprite));
                    continue;
                }
                if (!foundTitle && !foundTagString && line.StartsWith(entryTagIndicator))
                {
                    foundTagString = true;
                    tags[count] = sanitizeTagString(line.Substring(entryTagIndicator.Length).Trim());
                    continue;
                }
                if (currentTitle.Length > 0) currentTitle += '\n';
                currentTitle += line.Trim();
            }
            if (count > -1)
            {
                titles[count] = currentTitle.Trim();
                if (currentTitle.Length == 0)
                {
                    missingTitles++;
                    Debug.Log($"Missing title at index {count}");
                }
            }
            if (missingTitles > 0)
            {
                Debug.LogWarning($"Just a heads up, this playlist has {missingTitles} entries that don't have any titles.");
            }
        }

        private string pickle()
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < urls.Length; i++)
            {
                var url = urls[i];
                s.AppendLine(newEntryIndicator + url);

                var alt = alts[i];
                if (!string.IsNullOrWhiteSpace(alt.Get())) s.AppendLine(entryAltIndicator + alt);

                var image = images[i];
                if (image != null) s.AppendLine(entryImageIndicator + AssetDatabase.GetAssetPath(image.texture));

                var tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) s.AppendLine(entryTagIndicator + tag);

                var title = titles[i];
                if (!string.IsNullOrWhiteSpace(title)) s.AppendLine(title);

                s.AppendLine("");
            }
            return s.ToString();
        }

        private int recacheListPage()
        {
            var currentPage = currentFocus / perPage;
            var maxPage = rawEntryCount / perPage;
            currentPageStart = currentPage * perPage;
            currentPageEnd = Math.Min(rawEntryCount, currentPageStart + perPage);

            var pageLength = currentPageEnd - currentPageStart;
            currentPageUrls = new VRCUrl[pageLength];
            currentPageAlts = new VRCUrl[pageLength];
            currentPageTitles = new string[pageLength];
            currentPageTags = new string[pageLength];
            currentPageImages = new Sprite[pageLength];
            Array.Copy(urls, currentPageStart, currentPageUrls, 0, pageLength);
            Array.Copy(alts, currentPageStart, currentPageAlts, 0, pageLength);
            Array.Copy(titles, currentPageStart, currentPageTitles, 0, pageLength);
            Array.Copy(tags, currentPageStart, currentPageTags, 0, pageLength);
            Array.Copy(images, currentPageStart, currentPageImages, 0, pageLength);
            recachePage = false;
            return currentPageStart;
        }

        private void showListEntries()
        {
            if (recachePage) recacheListPage();
            var height = Mathf.Min(330f, perPage * 55f) + 15f; // cap size at 330 + 15 for spacing for the horizontal scroll bar
            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height)); // 1
            EditorGUI.BeginDisabledGroup(manualToImport); // 2
            for (var pageIndex = 0; pageIndex < currentPageUrls.Length; pageIndex++)
            {
                int rawIndex = currentPageStart + pageIndex;
                EditorGUILayout.BeginHorizontal();  // 3
                EditorGUILayout.BeginVertical();    // 4
                bool mainUrlUpdated = false;
                // URL field management
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"PC Url {rawIndex}", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var oldUrl = currentPageUrls[pageIndex] ?? VRCUrl.Empty;
                    var url = new VRCUrl(EditorGUILayout.TextField(oldUrl.Get(), GUILayout.ExpandWidth(true)));
                    if (url.Get() != oldUrl.Get())
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        urls[rawIndex] = url;
                        mainUrlUpdated = true;
                    }
                }

                // ALT field management
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"  Quest Url", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var oldAlt = currentPageAlts[pageIndex] ?? VRCUrl.Empty;
                    var alt = new VRCUrl(EditorGUILayout.TextField(oldAlt.Get(), GUILayout.ExpandWidth(true)));
                    if (mainUrlUpdated && autofillAltURL) {
                        alt = new VRCUrl(autofillFormat.Replace("$URL", urls[rawIndex].Get()));
                    }
                    if (alt.Get() != oldAlt.Get())
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        alts[rawIndex] = alt;
                    }
                }

                // TITLE field management
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("  Title", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var title = EditorGUILayout.TextArea(currentPageTitles[pageIndex], GUILayout.Width(250f), GUILayout.ExpandWidth(true));
                    if (title != currentPageTitles[pageIndex])
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        titles[rawIndex] = title.Trim();
                    }
                }

                // TAGS field management
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("  Tags", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                    var tagString = EditorGUILayout.TextArea(currentPageTags[pageIndex], GUILayout.Width(250f), GUILayout.ExpandWidth(true));
                    if (tagString != currentPageTags[pageIndex])
                    {
                        updateMode = ChangeAction.UPDATESELF;
                        tags[rawIndex] = sanitizeTagString(tagString);
                    }
                }

                EditorGUILayout.EndVertical(); // end 4
                var image = (Sprite)EditorGUILayout.ObjectField(currentPageImages[pageIndex], typeof(Sprite), false, GUILayout.Height(75), GUILayout.Width(60));
                if (image != currentPageImages[pageIndex])
                {
                    updateMode = ChangeAction.UPDATESELF;
                    images[rawIndex] = image;
                }
                if (!manualToImport)
                {
                    // Playlist entry actions
                    EditorGUILayout.BeginVertical(); // 4
                    if (GUILayout.Button("Remove"))
                    {
                        // Cannot modify urls list within loop else index error occurs
                        targetEntry = rawIndex;
                        updateMode = ChangeAction.REMOVE;
                    }

                    // Playlist entry ordering
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginDisabledGroup(rawIndex == 0);
                        if (GUILayout.Button("Up"))
                        {
                            targetEntry = rawIndex;
                            updateMode = ChangeAction.MOVEUP;
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.BeginDisabledGroup(rawIndex + 1 == rawEntryCount);
                        if (GUILayout.Button("Down"))
                        {
                            targetEntry = rawIndex;
                            updateMode = ChangeAction.MOVEDOWN;
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndVertical(); // end 4
                }
                EditorGUILayout.EndHorizontal(); // end 3
                GUILayout.Space(3f);
            }
            EditorGUI.EndDisabledGroup(); // end 2
            EditorGUILayout.EndScrollView(); // end 1
        }

        private string sanitizeTagString(string tagString)
        {
            // sanitize the tags to reduce the number of externs required for udon processing
            var tagList = tagString.Split(',');
            for (int k = 0; k < tagList.Length; k++)
            {
                var tag = tagList[k];
                tag = tag.ToLower();
                if (tag.Contains(":"))
                {
                    string tGroup = "", tValue = "";
                    int idx = tag.IndexOf(':');
                    if (idx > -1)
                    {
                        tGroup = tag.Substring(0, idx).Trim();
                        tValue = tag.Substring(idx + 1).Trim();
                        tag = tGroup + ':' + tValue;
                    }
                    else tag = tag.Trim();
                    tagList[k] = tag;
                }
                else tagList[k] = tag.Trim();
            }
            return string.Join(",", tagList);
        }

        #region Scene Updates

        private void updateScene()
        {
            Debug.Log($"Updating Scene. Mode {updateMode}");
            if (scrollView?.viewport == null)
            {
                Debug.LogError("ScrollRect or associated viewport is null. Ensure they are connected in the inspector.");
                return;
            }
            switch (updateMode)
            {
                case ChangeAction.ADD: addItems(); break;
                case ChangeAction.MOVEUP: moveItems(targetEntry, targetEntry - 1); break;
                case ChangeAction.MOVEDOWN: moveItems(targetEntry, targetEntry + 1); break;
                case ChangeAction.REMOVE: removeItems(targetEntry); break;
                case ChangeAction.REMOVEALL: removeAll(); break;
                default: break;
            }
            targetEntry = -1;
            switch (updateMode)
            {
                case ChangeAction.UPDATEVIEW:
                case ChangeAction.UPDATESELF: updateContents(); break;
                default: rebuildScene(); break;
            }
        }

        private void addItems()
        {
            var newIndex = urls.Length;
            Debug.Log($"Adding playlist item. New size {newIndex + 1}");
            urls = (VRCUrl[])AddArrayItem(urls);
            alts = (VRCUrl[])AddArrayItem(alts);
            tags = (string[])AddArrayItem(tags);
            titles = (string[])AddArrayItem(titles);
            images = (Sprite[])AddArrayItem(images);
            // Make sure the urls default to an empty instead of null
            urls[newIndex] = VRCUrl.Empty;
            alts[newIndex] = VRCUrl.Empty;
        }

        private void removeItems(int index)
        {
            Debug.Log($"Removing playlist item {index}: {titles[index]}");
            urls = (VRCUrl[])RemoveArrayItem(urls, index);
            alts = (VRCUrl[])RemoveArrayItem(alts, index);
            tags = (string[])RemoveArrayItem(tags, index);
            titles = (string[])RemoveArrayItem(titles, index);
            images = (Sprite[])RemoveArrayItem(images, index);
        }

        private void moveItems(int from, int to)
        {
            // no change needed
            if (from == to) return;
            Debug.Log($"Moving playlist item {from} -> {to}");

            urls = (VRCUrl[])MoveArrayItem(urls, from, to);
            alts = (VRCUrl[])MoveArrayItem(alts, from, to);
            tags = (string[])MoveArrayItem(tags, from, to);
            titles = (string[])MoveArrayItem(titles, from, to);
            images = (Sprite[])MoveArrayItem(images, from, to);
        }

        private void removeAll()
        {
            Debug.Log($"Removing all {urls.Length} playlist items");
            urls = new VRCUrl[0];
            alts = new VRCUrl[0];
            tags = new string[0];
            titles = new string[0];
            images = new Sprite[0];
        }


        #region Array Helper Methods
        protected System.Array NormalizeArray(System.Array stale, System.Type type, int normalizedLength)
        {
            if (stale == null || normalizedLength > 0 && stale.Length != normalizedLength) stale = System.Array.CreateInstance(type, normalizedLength);
            System.Array fresh = System.Array.CreateInstance(type, stale.Length);
            System.Array.Copy(stale, fresh, stale.Length);
            return fresh;
        }
        protected System.Array AddArrayItem(System.Array stale)
        {
            System.Array fresh = System.Array.CreateInstance(stale.GetType().GetElementType(), stale.Length + 1);
            System.Array.Copy(stale, fresh, stale.Length);
            return fresh;
        }

        protected System.Array RemoveArrayItem(System.Array stale, int index)
        {
            System.Array fresh = System.Array.CreateInstance(stale.GetType().GetElementType(), stale.Length - 1);
            System.Array.Copy(stale, 0, fresh, 0, index);
            System.Array.Copy(stale, index + 1, fresh, index, stale.Length - index - 1);
            return fresh;
        }

        protected System.Array MoveArrayItem(System.Array arr, int from, int to)
        {
            System.Object moving = arr.GetValue(from);
            // shift element leftward by shifting affected elements to the right
            if (to < from) System.Array.Copy(arr, to, arr, to + 1, from - to);
            // shift element rightward by shifting affected elements to the left
            else System.Array.Copy(arr, from + 1, arr, from, to - from);
            arr.SetValue(moving, to);
            return arr;
        }

        #endregion

        public void rebuildScene()
        {
            // determine how many entries can be shown within the physical space of the viewport
            calculateVisibleEntries();
            // destroy and rebuild the list of entries for the visibleCount
            rebuildEntries();
            // re-organize the layout to the viewport's size
            recalculateLayout();
            // update the internal content of each entry with in the range of visibleOffset -> visibleOffset + visibleCount and certain constraints
            updateContents();
            // ensure the attached scrollbar has the necessary event listener attached
            attachScrollbarEvent();
        }

        private void calculateVisibleEntries()
        {
            // calculate the x/y entry counts
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            var verticalCount = Mathf.FloorToInt(max.height / item.height) + 1; // allows Y overflow for better visual flow
            visibleCount = Mathf.Min(urls.Length, horizontalCount * verticalCount);
        }

        private void rebuildEntries()
        {
            // clear existing entries
            while (content.childCount > 0) DestroyImmediate(content.GetChild(0).gameObject);
            // rebuild entries list
            for (int i = 0; i < visibleCount; i++) createEntry();
        }

        private void createEntry()
        {
            // create scene entry
            GameObject entry = Instantiate(template, content, false);
            entry.name = $"Entry ({content.childCount})";
            entry.transform.SetAsLastSibling();

            var behavior = UdonSharpEditorUtility.GetBackingUdonBehaviour(script);
            var button = entry.GetComponentInChildren<Button>();

            if (button == null)
            {
                // trigger isn't present, put one on the template root
                button = entry.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var nav = new Navigation();
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
            }

            // clear old listners
            while (button.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(button.onClick, 0);

            // set UI event sequence for the button
            UnityAction<bool> interactable = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), button, "set_interactable") as UnityAction<bool>;
            UnityAction<string> switchTo = new UnityAction<string>(behavior.SendCustomEvent);
            UnityEventTools.AddBoolPersistentListener(button.onClick, interactable, false);
            UnityEventTools.AddStringPersistentListener(button.onClick, switchTo, nameof(script._SwitchToDetected));
            UnityEventTools.AddBoolPersistentListener(button.onClick, interactable, true);
            entry.SetActive(true);
        }

        private void recalculateLayout()
        {
            // ensure the content box fills exactly 100% of the viewport.
            content.SetParent(scrollView.viewport);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, 0);
            var max = content.rect;
            float maxWidth = max.width;
            float maxHeight = max.height;
            int col = 0;
            int row = 0;
            // template always assumes the anchor PIVOT is located at X=0.0 and Y=1.0 (aka upper left corner)
            // TODO enforce this assumption
            float X = 0f;
            float Y = 0f;
            // TODO Take the left-right margins into account for spacing
            // should be able to make the assumption that all entries are the same structure (thus width/height) as template
            Rect tmpl = ((RectTransform)script.template.transform).rect;
            float entryHeight = tmpl.height;
            float entryWidth = tmpl.width;
            float listHeight = entryHeight;
            bool firstEntry = true;
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform entry = (RectTransform)content.GetChild(i);
                // expect fill in left to right.
                X = entryWidth * col;
                // detect if a new row is needed, first row will be row 0 implicitly
                if (firstEntry) firstEntry = false;
                else if (X + entryWidth > maxWidth)
                {
                    // reset the horizontal data
                    col = 0;
                    X = 0f;
                    // horizontal exceeds the shape of the container, shift to the next row
                    row++;
                }
                // calculate the target row
                Y = entryHeight * row;
                entry.anchoredPosition = new Vector2(X, -Y);
                col++; // target next column
            }
        }

        private int calculateVisibleOffset(int rawOffset)
        {
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "page", account for the "extra" overflow row as well.
            var maxRow = (urls.Length - 1) / horizontalCount + 1;
            var contentHeight = maxRow * item.height;
            // clamp the min/max row to the view area boundries
            maxRow = Mathf.Min(maxRow, maxRow - verticalCount);
            if (maxRow == 0) maxRow = 1;

            var maxOffset = maxRow * horizontalCount;
            var currentRow = rawOffset / horizontalCount; // int DIV causes stepped values
            var steppedOffset = currentRow * horizontalCount;
            // currentOffset will be smaller than maxOffset when the scroll limit has not yet been reached
            var targetOffset = Mathf.Min(steppedOffset, maxOffset);

            // update the scrollview content proxy's height
            float scrollHeight = Mathf.Max(contentHeight, max.height + item.height / 2);
            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scrollHeight);
            if (scrollView.verticalScrollbar != null)
                scrollView.verticalScrollbar.value = 1f - (float)rawOffset / (maxOffset);

            return Mathf.Max(0, targetOffset);
        }

        private void updateContents()
        {
            int playlistIndex = calculateVisibleOffset(currentFocus);
            int numOfUrls = urls.Length;
            for (int i = 0; i < content.childCount; i++)
            {
                if (playlistIndex >= numOfUrls)
                {
                    // urls have exceeded count, hide the remaining entries
                    content.GetChild(i).gameObject.SetActive(false);
                    continue;
                }
                var entry = content.GetChild(i);
                entry.gameObject.SetActive(true);

                // track found components
                bool titleSet = false;
                bool urlSet = false;
                bool imageSet = false;

                // update entry contents
                Text[] textArr = entry.GetComponentsInChildren<Text>(true);
                foreach (Text component in textArr)
                {
                    if (!titleSet && component.name == "Title")
                    {
                        component.text = titles[playlistIndex];
                        EditorUtility.SetDirty(component); // this forces the scene to update for each change as they happen
                        titleSet = true;
                    }
                    else if (!urlSet && component.name == "Url" && showUrls)
                    {
                        component.text = urls[playlistIndex].Get();
                        EditorUtility.SetDirty(component); // this forces the scene to update for each change as they happen
                        urlSet = true;
                    }
                }

                Image[] imageArr = entry.GetComponentsInChildren<Image>(true);
                foreach (Image component in imageArr)
                {
                    if (!imageSet && (component.name == "Image" || component.name == "Poster"))
                    {
                        component.sprite = images[playlistIndex];
                        component.gameObject.SetActive(images[playlistIndex] != null);
                        EditorUtility.SetDirty(component); // this forces the scene to update for each change as they happen
                        imageSet = true;
                    }
                }
                playlistIndex++;
            }
        }

        private void attachScrollbarEvent()
        {
            var eventRegister = scrollView.verticalScrollbar.onValueChanged;
            // clear old listners
            while (eventRegister.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(eventRegister, 0);
            var playlistEvents = UdonSharpEditorUtility.GetBackingUdonBehaviour(script);
            var customEvent = new UnityAction<string>(playlistEvents.SendCustomEvent);

            UnityEventTools.AddStringPersistentListener(eventRegister, customEvent, nameof(script._UpdateView));
        }

        #endregion
    }
}