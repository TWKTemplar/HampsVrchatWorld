# ProTV Asset Changelog
All notable changes to this project will be documented in this file

Structure used for this document:
```
## Version Number (Publish Date)
### Added
### Changed
### Deprecated
### Removed
### Fixed
```

## 2.3.6 (2202-09-22)
### Fixed
- [ Core ] Looping was broken due to the mediaEnded flag not being cleared properly when the loop was triggered.
- [ Core ] _TvReady event should now correctly run once the TV has completed it initial waiting period instead of during the Start phase.
  - This should fix the issue of not being able to play a video from within _TvReady event.
- [ Core ] Late joiners should now properly respect the owner's play/paused state.
- [ Queue ] Fix certain edge cases where when multiple playlists are autoplay, they would all add something to the queue, resulting in excessive queued videos.
  - Now it should just utilize the highest priority playlist (as was the original intent of the integration)
- [ MediaControls ] Prevent accidental div-by-0. Fixes occasional instances where the seekbar would break entirely.

### Changed
- [ Core ] Do not set autoplay offset for TVs that are disabled by default.
  - This prevents excessive offsets when it's unnecessary as the offsets are specifically for avoiding rate-limit spam on-join.
- [ Playlist ] Use delayed media change if TV hasn't finished initializing.
  - This prevents improperly trying to play media before the TV is ready for it.

## 2.3.5 (2022-08-17)
### Fixed
- [ Core ] Use full namespace for the Stopwatch class in build helpers script to avoid naming collisions on compilation.

## 2.3.4 (2022-08-15)
### Added
- [ MediaControls ] Add `_UpdateMedia` event that will change URLs like usual, except that if one of the URLs are empty, it will default to the URL (main/alt respectively) to the one that is already present in the TV.
    - This enables being able to safely change the url for PC or Quest users without having to re-enter the URL for the other platform.
- [ Misc ] ProTV Box 3D model added.

### Fixed
- [ Core ] There were some occasions where when the URL is changed, the non-owners would be put into a paused state unexpectedly even though the TV was not in a locally paused state.
- [ Core ] Updated the build scripts to support arbitrary folder relocation for projects that require a custom folder structure.
- [ Core ] When a subscriber was disabled at start and enabled at some point later, the TV manager would erroneously call _TvReady on subscribers that already had that event called.
    - This fixes an issue where if you have plugins disabled at start (like playlists), when you enable them, it will no longer cause the TV to seek the time to 0 (the start of the media).
- [ Core ] Fix rare instances where media ends but does not trigger the media ending logic.
- [ Playlist ] Playlist autoplay on-load was skipping the queue entirely when a queue was provided.

## 2.3.3 (2022-08-02)
### Fixed
- [ Core ] The skip logic inadvertently triggered intermittently when a player joined, or when some other form of resync occurred.
    - This issue was inconsistent but occurred enough to reproduce and get a fix.
- [ Core ] Regression in recent logic changes that prevented proper media reloading on remote users when the owner clicks Stop then Play.
- [ Core ] Regression in recent logic changes that prevented the local pause from overriding the owner's sync state control.
    - The intended behavior is that when a remote user pauses a video, if the owner presses Pause then Play, the remote user won't be forced into the play state and instead retain the locally paused state as before.
- [ MediaControls ] Quest URL input game object was not being properly hidden for non-privileged users when tv became locked.

## 2.3.2 (2022-07-31)
### Changed
- [ Docs ] Version numbers now use semantic version syntax.

### Fixed
- [ Core ] Calling _IsPrivilegedUser during the start event would sometimes return false due to the TV not having been initialized.
- [ Core ] Extraneous reload occurred for users after lock being called by a user who wasn't the current tv owner.
- [ MediaControls ] Lock button should always be visible to users so they can see if the TV is in a locked state.
    - This is to prevent confusion for users who would otherwise be unable to determine on their own why they are unable to play any media.
- [ MediaControls ] Seek slider on VertControls (Retro) was not using the correct layout settings.
- [ Queue ] TV lock state was not always being respected.
    - While the TV is locked, only privileged users should be able to queue media.
    - Existing media will remain and can be removed by respective users, but new media should not be added by unprivileged users while locked.
- [ VoteSkip ] Was not always respecting the TV lock state.
- [ VoteSkip ] Clarified text display to represent the tv's locked state meaning voting is disabled.
    - While TV is locked, any privileged user can immediately skip the media. The vote ratio is ignored while in the locked state.
    - The voting ratio is hidden while the TV is in the locked state if the user is unprivileged.

## 2.3.1 (2022-07-28)
### Added
- [ MediaControls ] Enable having the media input field send the URL to a queue if available instead of immediately playing it.
    - This bring parity with the Playlist being able to do so as well.
- [ Core ] Added missing sprite atlas for the retro theme icons.

### Changed
- [ Docs ] Updated MediaControls documentation for recent changes.

### Fixed
- [ Prefabs ] Corrected certain missing references.

## 2.3 Stable Release (2022-07-27)
### Changed
- Version bump for release

## 2.3 Beta 3.15 (2022-07-27)
### Added
- [ Core ] Option for specifying if the TV should locally stop or pause the currently active media when it becomes disabled.

### Changed
- [ Demo Scene ] Clean up old references and make things more in-line with the latest prefabs available.

## 2.3 Beta 3.14 (2022-07-26)
### Changed
- [ Core ] Reworked the logic for handling the disable/enable state of the TV itself to use the ownerError control logic.
    - This means that owners can now safely disable the TV itself without forcing everyone else to pause!
    - It also tracks when the owner disabled the game object, and when they enable it, it jumps to where the media should have been if they hadn't disabled it so it doesn't interrupt other's viewing experience!

## 2.3 Beta 3.13 (2022-07-25)
### Added
- [ MediaControls ] New 2000's retro UI variations! Includes variations of the Standard and Advanced controls!
- [ Branding ] New QR code images for use in worlds. Is also included in the `AssetInfo` prefab by default.

### Changed
- [ Core ] Moved all UI icons from different locations into a central UI folder.
- [ MediaControls ] Simplified some handling of main/alt url switching logic.
- [ MediaControls ] Renamed 'ClassicControls' to 'StandardControls' as the term Classic is now being used as a style category term rather than a specific prefab reference.

### Fixed
- [ Playlist ] Autofill Quest Urls option was not properly autofilling the very last entry in a given playlist.
- [ Playlist ] After switching to playlist search using text box, on PC the input field would always lag 1 character behind due to the UI events being called before the associated text component was updated. Should work correctly now.


## 2.3 Beta 3.12 (2022-07-21)
### Changed
- [ Playlist ] Updated playlist search to use the text boxes of the input urls instead of the input URLs themselves to (hopefully) allow quest to be able to run searches.
    - If you are implementing a custom keyboard for the search boxes, you will need to remove the Input Field component off the title/tags searches, and switch to modifying the Text component + explicitly calling the `_UpdateSearch` event on the PlaylistSearch script directly.

## 2.3 Beta 3.11 (2022-07-21)
### Added
- [ Core ] New `_Skip` method which is used to force the media to finish.
    - Supports both livestreams and fixed-length videos.
    - Ignores non-owners (the trigger is based on syncTime data).
    - Privileged users can call this and will take ownership before skipping.

### Changed
- [ VoteSkip ] Updated trigger logic to call new `_Skip` method instead of forcing the seek time.
    - This enables proper support for skipping livestreams.

## 2.3 Beta 3.10 (2022-07-20)
### Changed
- [ Prefabs ] Fixed broken references to the playlist in the PlaylistQueueDrawer caused by the previous beta.

## 2.3 Beta 3.9 (2022-07-20)
### Added
- [ Core ] New build script logic which automatically updates the options of any MediaControls dropdowns.
    - This removes the need to update the count and labels of the dropdown options.
- [ Core ] VideoManagers now have a custom label you can specify.
    - Currently this is primarily used by the MediaControls for auto-populating video player swap dropdown options.
- [ Core ] New `defaultRetryCount` field for specifying an implicit retry count per-TV instead of per-URL.
- [ Core ] New build helper method to automatically update any MediaControls dropdowns.
- [ MediaControls ] New `customLabel` field which allows you to specify a custom name for the video manager which will then be populated in the dropdowns during the build phase.
- [ Playlist ] Playlists can now autofill the alternate/quest url based on a given format string.
    - In this format string, the main URL will be injected where the special value `$URL` is.
    - eg: If main url is `https://youtu.be/VIDEO_ID` with the autofill format of `https://mydomain.tld/?url=$URL` the result would be `https://mydomain.tld/?url=https://youtu.be/VIDEO_ID`

### Changed
- [ Core ] Reordered the properties on the TVManagerV2 script for better organization, also added new section headers.
- [ Core ] Restored the `startHidden` option as it is still useful for niche situations.
- [ Core ] Enabled local user to be considered a privileged user ONLY IF the tv is currently NOT syncing to the owner.
- [ Queue ] Update prefabs to have a color tint effect on the queue media and next media buttons.
- [ Prefabs ] Corrected a couple improper references.
- [ Prefabs ] In the PlaylistQueueDrawer, moved the Playlist script onto a game object outside of the toggled parent game object so that it will always properly initialize for autoplay without needing to manually switch the tabs to it.
- [ Docs ] Added missing release dates to various 2.3 beta releases in the CHANGELOG.
- [ Docs ] Updated various documents related to changes in the 2.3 release.

## 2.3 Beta 3.8 (2022-07-18)
### Changed
- [ Core ] Added sanity null checks against the strings contained within VRCUrls for rare instances where unity serialization messes up
- [ Core ] Rename prefab `ProTV Modern` to `ProTV Advanced` for better clarification on the contents of the prefab
- [ MediaControls ] Rename prefab `ModernControls` to `AdvancedControls` for better clarification on the contents of the prefab

## 2.3 Beta 3.7 (2022-08-18)
### Added
- [ Queue ] New `_Purge` event will remove all entries that a player has control over.
    - If a privileged user calls this event, it will wipe the whole queue.
    - If a non-privileged user calls this event, it will remove all of that player's particular entries.

### Changed
- [ Core ] Updated sync data to notify when the owner's video had failed, and ignore certain sync info until a proper change occurs.
    - This should alleviate confusion and issues that are caused by media failing on the owner's side.
    - eg: If a youtube video is loaded when a quest user was the owner, it'd fail for them causing the video to stop for everyone else.
    - NOTE: This will cause an intentional partial desync because the owner media sync fails.  
    - If you wish to re-enable full sync without changing videos, you will need a privileged user to either lock, unlock, or reload the video.
- [ Core ] Security improvements.

## 2.3 Beta 3.6 (2022-07-17)
### Added
- [ Core ] New toggle setting for choosing the initial audio mode between 2D(Stereo) and 3D(Spacialized)
- [ Core ] Add 3D support with the new `ProTV/VideoScreen3D` shader.
    - Includes some auto-detection logic for switching between 2D/3D video rendering.
    - Logic is an aspect ratio threshold check, which is adjustable in the shader properties.  
    Defaults to a ratio of greater than 2:1 as the threshold.
    - All provided screen materials now default to this shader.
    - The old shader `ProTV/VideoScreen` is retained for backwards-compatibility.

### Fixed
- [ Core ] Extra null checks against badly formed VRCUrls in certain edge-cases.
- [ Core ] Tentative fix for random edge-case of video failure when a player joins.
- [ Core ] Tentative fix for MediaControls having the input field not being unhidden during media retry if the media fails (eg: livestream offline).

## 2.3 Beta 3.5 (2022-07-11)
### Fixed
- [ AudioLinkAdapter ] Fixed compiliation error when audiolink is not present in the project

## 2.3 Beta 3.4 (2022-07-11)
### Changed
- [ Core ] Change the default value for `retryWithAlternateUrl` to `true`

### Fixed
- [ Core ] Restored missing reference to the MediaInput element in the PlaylistQueueDrawer prefab

## 2.3 Beta 3.3 (2022-07-10)
### Changed
- [ Playlist ] Improved handling of error failures to be more reliable against various edge cases
- [ Playlist ] Reduced excessive reloading of already failed urls when `retryWithAlternateUrl` is enabled

### Fixed
- [ Core ] There were certain situations where the `_TvMediaChange` event was not being triggered on loading a URL
- [ Core ] Added conditional checks for properly handling time jumping when `retryWithAlternateUrl` is enabled
    - This fixes an issue of the media sometimes skipping the first few seconds of the media.

## 2.3 Beta 3.2 (2022-07-10)
### Fixed
- [ Playlist ] Tentative fix for certain edge cases where url failures do not proceed to the next entry correctly

## 2.3 Beta 3.1 (2022-07-10)
### Changed
- [ Core ] Moved the logo image used in the `AssetInfo` from the Docs/Branding folder to the Images folder for being consistent about its use.

### Removed
- [ Core ] Temporarily removed the ProTVUtils script as it is not being used by any existing part of ProTV at the moment.
    - This should fix U# 1.x failing to compile

## 2.3 Beta 3.0 (2022-07-08)
### Added
- [ Core ] Now checks for a url query parameter which typically indicates a proxy service is being used (eg: Jinnai, Qroxy).
    - If detected, the proxied URL domain will be used for the tv's localLabel value instead of the original url domain.
- [ Core ] Add new `AssetInfo` prefab which will automatically display the current version of ProTV along with the gumroad and discord links.
    - Prefab located at `Assets/ArchiTechAnon/ProTV/Prefabs/Misc/AssetInfo`
    - This prefab is embedded by default in each of the MediaControls prefabs behind a very small toggle button, so it generally stays out of the way.
    - The particular toggle icon will be updated to something more appropriate in a future update.
    - The version number in the `AssetInfo` prefab will automatically update whenever you build the world.
    - More specifically, any scene object named `ProTV Version` with a Text component attached will be updated with the version number on build.
- [ Core ] Add new advanced options flag `retryWithAlternateUrl` to allow specifying if you want to automatically try the other URL (main/alt) if the one attempted returned an error.
    - Will flip the flag internally to attempt the other URL (graceful fallback for missing urls still apply).
    - Then if that too fails, it will swap back until the original URL succeeds or a new URL is input.
- [ Core ] Make retry count default to 1 if `retryWithAlternateUrl` is enabled
- [ Core/Shader ] Horizontal auto-flipping in mirrors has been added.
- [ Skybox/Shader ] Vertical flip issues have been corrected.

### Changed
- [ Core/Shader ] Rename `Video/RealTimeEmmisiveGammaWithAspectRatio` to `ProTV/VideoScreen`
- [ All Plugins ] Slight improvement to how the default plugins handle error messaging when the TV reference is missing.
- [ Skybox/Shader ] Rename `Video/Skybox` to `ProTV/Skybox`
- [ MediaControls ] Consolidated logic for `_ChangeAltMedia` into `_ChangeMedia` so the latter now cleanly handles both main and alt url inputs.
- [ MediaControls ] Reworked the `AlternateUrlControls` into a more useful, general-purpose `UrlControls` prefab.
- [ MediaControls ] Fixed some minor issues with the DrawerControls animator.
- [ Playlist ] Improved scroll behaviour for existing prefabs.
- [ Queue ] Improved scroll behaviour for existing prefabs.

### Fixed
- [ MediaControls ] Use new VRCUrl instance instead of the global VRCUrl.Empty instance for the field defaults in QuickPlay script.

### Removed
- [ Core ] Temporarily removed the custom editors for VideoManager and TVManager. These will be rebuilt for the next stable version.

## 2.3 Beta 2.5 (2022-05-27)
### Fixed
- [ Core ] Custom editor for video manager wasn't calling the base.OnInspectorGUI method thus was not populating the inspector.

## 2.3 Beta 2.4 (2022-05-27)
### Added
- [ Core ] Loading timeout limit for preventing infinite loading states

### Fixed
- [ Core ] Fixed stupid typos for usage of the word 'privilege' from the incorrect spelling of 'priviledge'


## 2.3 Beta 2.3 (2022-05-26)
### Added
- [ Core ] Added primitive whitelist name check. Whitelist is available on the TVManagerV2ManualSync object.
    - NOTE: The location of the whitelist is subject to change and notice will be given if it does. The current location is considered a temporary stop-gap until a better solution gets implemented.
    - This whitelisted users have the same control priviledge as the instance master, except that they are not beholden to the `allowMasterControl` check.
    - Instance Owner will always have priviledge over everyone else, as usual.
- [ Playlist ] Add some helper getters for certain data for U# scripts
    - `SortView` returns the list of indices for the current sort order of the playlist
    - `FilteredView` returns the list of indices for the current search filter applied to the playlist (hidden indices won't be present in this array)
    - `CurrentEntryIndex` returns the original playlist entry index that has been detected. If no entry is detected to be active, -1 will be returned.
    - `NextEntryIndex` returns the original playlist entry index for the next expected entry. Primarily used for autoplay mode.
    - `CurrentEntryMainUrl` returns the main VRCUrl for the current active detected entry. Returns VRCUrl.Empty if no entry is active.
    - `CurrentEntryAltUrl` returns the alternate VRCUrl for the current active detected entry. Returns VRCUrl.Empty if no entry is active.
    - `CurrentEntryTags` returns the tag string for the current active detected entry. Returns string.Empty if no entry is active.
    - `CurrentEntryInfo` returns the description (aka title) for the current active detected entry. Returns string.Empty if no entry is active.
    - `CurrentEntryImage` returns the Sprite type image for the current active detected entry. Returns null if not entry is active.

### Changed
- [ Core ] Cleaned up internal usage and exposed a debug flag which will disable logging for a given TV and it's plugins when unchecked.
- [ Core ] Added error message when the manual sync data component is not present.
- [ Core ] Renamed `_CanTakeControl` to `_IsPrivilegedUser` to better describe the purpose of the logic. The method signature for `_CanTakeControl` will simply call the new `_IsPrivilegedUser` method signature for backwards compatibility.
- [ AudioLinkAdapter ] AudioLinkAdapter is now a first-class plugin. It is now located in the standard `ProTV/Plugins` folder!
    - NOTE: Certain changes to make this work *REQUIRES* AudioLink 0.2.8. Earlier version will not properly detect the AudioLink asset.

### Fixed
- [ MediaControls ] Fix lock not showing for instanceOwner correctly under certain conditions
- [ Playlist ] Fix starting on a random entry sometimes causing an array out of bounds failure

## 2.3 Beta 2.2 (2022-05-13)
### Fixed
- [ Core ] Fix default values for video swap input variables to be the correct -1 so that swapping to index 0 works correctly

## 2.3 Beta 2.1 (2022-05-07)
### Added
- [ Core ] Add prefab that specifically uses the UnityPlayer with a RenderTexture
- [ Core ] Add `_TogglePlay` event to be able to use a single event to switch between pausing and playing.
    - If stopped, it will attempt to reload the current media.

### Changed
- [ Docs ] Update README to better reflect the 2.3 beta 2.0 changes.


## 2.3 Beta 2.0 (2022-05-06)
### Added
- [ Core ] Add support for loop/start/end/t params to be defined via the url hash (after the #, more in the docs)
- [ Core ] Add hash param `live` which declares a url is expected to be live media. This helps signal how to handle errors for media that isn't loaded properly.
- [ Core ] Add hash param `retry` which declares how many times a url should be retried before signalling that an error has actually occurred.
    - If set to `-1`, the TV will infinitely retry the url, with 15 second intervals. Useful for livestreams.
    - Any number greater than `0` will make the TV attempt to reload the video up to that number of times before moving on.
- [ Core ] Add autoplay label field to have the autoplay urls replaced with custom text on UIs
- [ Core ] Add _EnableInteractions/_DisableInteractions/_ToggleInteractions to enable a global interaction toggle for any attached subscribers
    - It goes through the event subscribers and searches for all VRCUIShapes, then finds any attached colliders and then either disables or enables them.
    - This makes it so the player's raycast pointer either does or does not interact with it.
- [ MediaControls ] Add alternate url support to the QuickPlay script
- [ Playlist ] Add explicit warning when a playlist has no entries present
- [ Playlist ] New playlist tagging
    - A metadata field for putting search terms in for each playlist entry. Never displayed
- [ Playlist ] New playlist search syntax, supports searching through tags, titles and urls
    - Search for combined terms using a plus (+) and search for optional terms using a comma (,)
        - Eg: "animated+1999,animated+2000" could be used to find animated movies in 1999 or 2000
- [ Playlist ] Add option for a playlist to send the selection to a queue instead of playing immediately
    - To enable this mode, simply connect a Queue component to the Playlist queue slot.
- [ Playlist ] Add alternate url support, including with the file format. Alternate urls are prefixed with `^`.
- [ Playlist ] Add new Save button to bring up a file save menu for much quicker playlist exporting.
    - This will also automatically update the playlist to switch to import mode with the freshly saved playlist assigned.
- [ Playlist ] Add new PlaylistData type for offloading playlist entries onto a separate game object.
    - This helps manage performance issues for VERY LARGE playlists by moving the unity serialization issues onto a game object that the regular playlist script isn't on.
    - Completely optional, will default to use the same game object if PlaylistData is not present.
- [ Queue ] Add alternate url support
- [ VoteSkip ] New VoteSkip plugin and prefab
    - Includes a VoteZone component for handling spacial areas in which the VoteSkip can be used by.
    - This is to make it so that not everyone in the world needs to be involved with the vote skip, just those
    that are currently paying attention to the TV (ie: being inside one of the VoteZones defined).
    - If no VoteZones are provided in the world, then VoteSkip will simply use the global player count in the world
- [ Docs ] Added Docs file for VoteSkip

### Changed
- [ Core ] Renaming/restructuring old prefabs:
    - ProTV Slim > ProTV Classic
    - ProTV Music Player > ProTV Music
    - ProTV Modern Model > ProTV Modern
- [ Core ] Looping is now specified with a count
    - If `loop=0` or if loop is not present in the url, no looping will occur
    - If `loop=-1` (or any other negative number) or if `loop` is present without a value, looping will be infinite
    - Otherwise the video will loop the given number of times after the inital playthrough
    - `loop=1` means the video will actually play just 2 times
- [ Core/Security ] Instance owner now properly overrides control of master, so when the instance owner locks the TV, the master is no longer able to implicitly unlock it.
- [ MediaControls ] Improved the error messaging between live and fixed length media. Should be a bit more clear as to the actual problems that occur.
- [ MediaControls ] TV error messages now respect the showVideoOwner(ID) fields
- [ MediaControls ] Improve error messaging for livestream media
- [ Playlist ] Fix handling of video errors. Only process once the TV has signaled that an error actually occurred.
- [ Playlist ] Searching over multiple frames (async) enabled
- [ Playlist ] (Internal) All references to "rawView" have been converted into "sortView" to more properly align the naming with what that cache is accomplishing.
- [ Playlist ] Renamed `AncientPlaylist` to `FlatPlaylist` and renamed `SlimPlaylist` to `ClassicPlaylist`
- [ Docs ] Updated Changelog to be more organized, added historical dates
- [ Docs ] Moved READMEs for plugins into the Docs folder so they aren't spread across the folders. Renamed the files according to the respective plugins.

### Deprecated
- [ Core ] Deprecating old prefabs:
    - ProTV Legacy Model Extended
    - ProTV Legacy Model
- [ Core ] Deprecate use of loop/start/end/t in the query parameters in favor of having them in the url hash
- [ Core ] Deprecate the longform IN variables in favor of shorter more memorable variable names:
    - `IN_ChangeMedia_VRCUrl_Url` -> `IN_URL`
    - `IN_ChangeMedia_VRCUrl_Alt` -> `IN_ALT`
    - `IN_ChangeVideoPlayer_int_Index` -> `IN_VIDEOPLAYER`
    - `IN_ChangeVolume_float_Percent` -> `IN_VOLUME`
    - `IN_ChangeSeekTime_float_Seconds` -> `IN_SEEK`
    - `IN_ChangeSeekPercent_float_Percent` -> `IN_SEEK`
    - `IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber` -> `IN_SUBSCRIBER`
    - `IN_RegisterUdonEventReceiver_byte_Priority` -> `IN_PRIORITY`  
    These deprecated variables will still work as expected, but will be removed in a later version.

### Removed
- [ Core ] Remove duplicated isMaster call during init causing the script to fail during publish.
- [ Core ] Removed `retryLiveMedia` as the new `retry` and `live` hash params replace that behaviour
- [ Core ] Removed the longform OUT variables in favor of shorter, more memorable ones:
    - `OUT_TvVideoPlayerError_VideoError_Error` -> `OUT_ERROR`
    - `OUT_TvVolumeChange_float_Percent` -> `OUT_VOLUME`
    - `OUT_TvVideoPlayerChange_int_Index` -> `OUT_VIDEOPLAYER`
    - `OUT_TvOwnerChange_int_Id` -> `OUT_OWNER`  
    These longform variables will no longer receive the data and will need to be updated to the new short form to continue working properly.
- [ Playlist ] Removed `Skip to next entry on error` option in favor of it being implicit behavior by default (always enabled).

### Fixed
- [ Core ] Certain edge cases where the start/end params wouldn't work properly
- [ Core ] Immediately after join if video is active, a non-owner would be unable to play/pause locally
- [ Core ] Video Swap not working for late joiners
- [ Core ] Video Swap not retaining the current playing timestamp properly (aka lossless reload)
- [ Core ] Fix audio/video resync not working for livestreams
- [ Playlist ] Lists with 1 entry were not looping correctly.
- [ Playlist ] No longer crashing when 0 entries are present
- [ Queue ] Behaviour no longer crashes when the next media button is not provided.
    - This allows a world creator to have an add-only queue, making it easier to manage via the new VoteSkip plugin.
- [ Queue ] Fix crashing on quest when the title input field was accessed in the code.
    - Because apparently, VRC destroys the InputField components on quest as the way to prevent the keyboard from showing up.
    Why? No idea but that's what's been observed, so we need to do null checks for that stuff.

### <span style="color:cyan">Upgrading from previous versions</span>
If you have imported a previous version of ProTV (2.3 Beta 1.0 or earlier), after importing this version (2.3 Beta 2.0 or later) you will need to do two things.  
1) If you have any playlists in the world, simply click on them to show their inspector, then toggle any of the flag options. This is to make it so that the searialized data gets updated to the new struture with tags and alt urls.  
2) If you have any queue's in the world, you'll want to delete those and drag in a new copy. Queue has been reworked a bit plus it now has 20 entries in it.  
This includes the ProTV Composite prefab (now known as ProTV Hangout). You'll want to replace the copy in your scene with a new one since it has a Queue integrated into it.  

That should be it.


## 2.3 Beta 1.0 (2022-03-05)
### Fixed
- Fix loop not working properly on TVs that aren't synced to owner
- Tentitive fix for certain situations where synced looping wasn't working


## 2.2 Stable Release (2022-01-24)
### Changed
- Version bump for release


## 2.2 Beta 3.1 (2022-01-22)
### Added
- Add Playlist option for specifying if autoplay should start running on load or wait until interaction: `Autoplay On Load`
- Add Playlist option for specifying if autoplay should make the playlist restart if it reaches the end: `Loop Playlist`


## 2.2 Beta 3.0 (2022-01-18)
### Added
- Add an udon graph compatible `_Switch` event for the playlist, utilizing new variable `SWITCH_TO_INDEX`
- Add optional buffer time for allowing a video to pre-load before playing
- Add descripive text to CompositeUI detailing how to interact with the visibility

### Changed
- Update Resync (Micro) to use the correct icon

### Fixed
- Fix inconsistent animations on the UI for ProTV Composite
- Fix MediaControls info data not being updated at all the points it was supposed to
- Fix owner reloading not doing proper jumpTime to the active timestamp (aka lossless owner video reload)
- Fix skybox settings UI interactions not working as expected
- Fix video player selection sync being improperly implemented
- Updated documentation for new changes


## 2.2 Beta 2.1 (2021-12-14)
### Changed
- Adjust GeneralQueue so that there isn't any conflicting sync types

### Fixed
- Attempts at fixing Unity being absolutlely insufferable with broken references


## 2.2 Beta 2.0 (2021-12-04)
### Added
- Add optional toggle for syncing the current video player selection.

### Changed
- Udpate logic for handling consistent setup of the canvas colliders.
    - If you have your own UI or you unpacked a prefab, you will need to add the new script `ProTV/Scripts/UiShapeFixes` to any canvas gameobject with a `VRCUiShape` component on it. If you are using the prefabs, they should automatically update with the new script.

### Removed
- Remove some udon overhead by switching some scripts to None sync type.


## 2.2 Beta 1.1 (2021-11-10)
### Added
- Add functionality to MediaControls url inputs to be able to submit the URL upon pressing enter.


## 2.2 Beta 1.0 (2021-11-08)
### Added
- Add new Misc folder and new prefab "PlaylistQueueDrawer". Contains new visuals for playlist and queue in a drawer like layout.
- Add new MediaControls prefab "DrawerControls".
- Add new TV Prefab "ProTV Composite". Contains both "DrawerControls" and "PlaylistQueueDrawer" prefabs.
- Add configurable player-specific limit value to the Queue plugin.
- Add support for changing and shifting priorities for event subscribers.
	- First (before all other priorities)
	- High (first of its current priority)
	- Low (last of its current priority)
	- Last (after all other priorities)
- Add udon events for interacting with the new priority shifting mechanism.
- Add playlist integration with priority shifting. This allows for a playlist to prioritize itself when interacted with. Set the new `Prioritize on interact` flag under the autoplay section of the playlist script to enable.

### Changed
- Update Queue to utilize array sync for urls (since VRChat recently added support for that).
- Update URL resolver shim to look for the new ytdlp executable.
- Update playlist init code to prefer the TV's autoplay url field over its own list.

### Fixed
- Fix Queue plugin causing videos to abruptly stop when certain players leave the world.
- Fix looping via play button after video ends causing extraneous events to trigger that shouldn't.
- Fix race condition with seeking where it wouldn't always seek to the desired time depending on when the seek is requested.


## 2.1 Stable Release (2021-10-31)
### Changed
- Version bump for release


## 2.1 Beta 3.0 (2021-10-08)
### Changed
- Updated VideoManger to have separate lists for managed and unmanaged speakers and screens.
    - This is intended to allow for more fine grained control of what the video managers should actually affect.
    - Immediate use case is for dealing with audio link speakers(they are typically at 0.001) that you don't want the volume changed on, but still want to have the TV control the auible speakers' volume.
- Updated example scene to reflect changes.

### Removed
- Removed the autoManageScreenVisibility flag.
    - With the new Unmanaged Screens list, this flag is duplicated functionality.

### Notes
- Existing TV setups should still work as expected after importing. There is fallback logic that exists for handling the previous references that Unity already has serialized. The only exception is if you set the autoManageScreenVisibiliy flag to prevent the VideoManager from controlling the referenced screens. To fix, you will need to add that screen to the Unmanaged Screens list for the same behaviour.


## 2.1 Beta 2.9 (2021-10-14)
### Added
- Add additional null check for the Queue plugin to remove unintended error occuring in non-cyanemu playmode (like build ; publish)


## 2.1 Beta 2.8 (2021-10-14)
### Changed
- Adjustments to the Modern Model and Legacy Model prefabs for the options available. This clarifies some options as well as includes a default Unity player option in the general controls options list. Legacy Model Extended still retains the original list from the example scene.

### Fixed
- Fix playlist unintentionally scrolling when contained within a pickup object (something that moves the playlist's world postition)
- Fix timestamp not being preserved as it used to during a video player swap.


## 2.1 Beta 2.7 (2021-10-13)
### Added
- Add null checks to remove unintended errors occuring in non-cyanemu playmode (like build ; publish)


## 2.1 Beta 2.6 (2021-10-13)
### Added
- Add null check to the video swap dropdown layering check incase the dropdown isn't a direct child of it's associated canvas object. This avoids an incedental script crash, but can cause odd layering issue if it's not a direct child, so be careful.


## 2.1 Beta 2.5 (2021-10-12)
### Added
- Add events on the playlist to manage the autoplay mode

### Fixed
- Fix bad execution ordering between TVManagerV2 and TVManagerV2ManualSync scripts


## 2.1 Beta 2.4 (2021-09-28)
### Fixed
- Fix layering issues and pointer issues with the general controls video swap dropdown


## 2.1 Beta 2.3 (2021-09-27)
### Added
- Add explicit Refresh button to the slim UI prefab.
- Add new VertControls UI prefab. Similar to Slim UI, but layout is vertical with some elements removed.

### Changed
- Updated the icon for the Resync button on the slim UI prefab to be distinct from the Refresh button.

### Removed
- Remove forced canvas sort order for controls UIs.


## 2.1 Beta 2.2 (2021-09-14)
### Added
- Add warning message when a playlist import detects one or more entries that do not have a title associated with it. This is just an alert and can be ignored if the missing titles are intentional.

### Changed
- Minor performance improvment to the playlist editor script

### Fixed
- Fix playlist not updating the TV's localLabel on non-owners
- Fix attempt for videos having issues with looping (stutter and occasionally unexpected pausing)
- Fix playlist producing null titles instead of empty strings causing the search feature to fail


## 2.1 Beta 2.1 (2021-09-07)
### Changed
- Change `useAlternateUrl` to be not exposed to the inspector. It is still a public variable for runtime though.
- Change url logic to have quest default to the alternate, with fallback to main url when alternate is not provided (this allows seemless backwards compatibility)


## 2.1 Beta 2.0 (2021-09-04)
### Added
- Add first class support for alternate urls. This alleviates issues with requiring separate URLs for each platform (notably VRCDN)
- Add toggle prefab to allow switching between Main and Alt urls. This is _HIGHLY_ recommended to have in-world if you make use of the alternate url feature.
- New events related to alternate urls: `_UseMainUrl`, `_UseAltUrl`, `_ToggleUrl`
    - BE SURE TO CHECK YOUR SCENE REFERENCES TO ENSURE THEY ARE CONNECTED PROPERLY. Some variable names changed related to urls and certain references _may_ have become disconnected.

### Changed
- Update `PlayURL (Micro)` prefab to support alternate url. Great for predetermined stream splitting.

### Fixed
- Fix playlist not always correctly representing the loading bar percent while scrolling.
- Fix certain issues with the skybox playlist not working properly.


## 2.1 Beta 1.1 (2021-09-01)
### Added
- Add playlist shuffling via `_Shuffle` event.
- Add option to automaticially shuffle the playlist on world load (currently not synced).
- Add option to start autoplaying at a random index in the playlist.
- Add playlist view automatically seeking to the current index on world load (complements the random index start).

### Changed
- Additional internal state caching improvements for playlist
- Some code golfing micro-optimizations


## 2.1 Beta 1.0 (2021-08-29)
### Added
- Add playlist pagination prefab.
- Add U#'s URL Resolver shim for playmode testing with the unity player (AVPro still doesn't work in-editor yet)

### Changed
- Improve some internal state caching for playlist

### Fixed
- Fix scrollbar not resizing with the playlist when a filter is applied (aka playlist search)


## 2.0 Stable Release (2021-08-26)
### Changed
- Version bump for release


## 2.0 Beta 8.5 (2021-08-22)
### Changed
- Update automatic resync interval to Infinity if value is 0 (both values should represent the same effect).
- Mitigations for when the TV starts off in the world as disabled. Should be able to just toggle the game object at will, though if you want the TV to start off as disabled, make sure the game object itself is off instead of relying on other scripts to toggle it off for you (like a toggle script). There is a known bug with having it on and disabling it during Start. See and upvote: https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/1123-udon-objects-with-udon-children-initialize-late-despite-execution-order-ove
- Forcefully disable the built-in auto-resync cause it breaks things reeeee
- Improve the skybox options in the demo scene.
- Update playlist structure and logic for vastly improved performance at larger list sizes.


## 2.0 Beta 8.4 (2021-08-08)
### Added
- Add skybox support for CubeMap style 360 video.
- Add skybox support for 3D video modes SideBySide and OverUnder.
- Add skybox support for brightness control.
- Add settings UI to the skybox TV prefab.
- Add custom meta support for the URLs. 
    - Can now specify custom data that is arbitrarily stored in the TV in the `string[] urlMeta` variable.
    - All meta entries are separated by a `;` and proceeds a hash (`#`) in the URL.
    - Example: With a url like `https://vimeo.com/207571146#Panoramic;OverUnder`, the `urlMeta` field will contain both `"Panoramic"` and `"OverUnder"`.
    - This meta portion of the URL can be used for pretty much anything as anything as the hash of a URL is ignored by servers. Use it to store information about any particular individual url (such as what skybox modes to apply).

### Changed
- Updated demo scene with new skybox data.

### Fixed
- Fixed entry placement regression in playlist auto-grid.


## 2.0 Beta 8.3 (2021-08-07)
### Fixed
- Fixed playlist auto-grid being limited to 255 rows or columns. Should be able to have many more than that now.
- Fixed playlist in-game performance issues by swapping from game object toggling to canvas component toggling.
    - This specifically fixes lag issue when desiring to hide the playlist.   
    While game object toggling is still supported, this new mode is highly recommended. Is utilized by calling `playlist -> _Enable/_Disable/_Toggle` events.
    - Playlist Search also makes use of this performance improvements by having a canvas component on the template root object (and thus on every playlist entry object).


## 2.0 Beta 8.2 (2021-08-05)
### Added
- Added KoFi support links to the Docs. Support is inifinitely appreciated!
- Added Micro style controls to the MediaControls plugin.
- Added a one-off play url button control to the MediaControls plugin. This has definitely been requested quite a bit.

### Changed
- Cleaned up names of prefabs a bit (no breaking changes)
- Exported with 2019 LTS


## 2.0 Beta 8.1 (2021-07-30)
### Added
- Added better support for plugins being disabled by default getting enabled after the world load phase.
    - This guarantees that AT LEAST the `_TvReady` event will _ALWAYS_ be the first event called on a subscribed behavior.
- Add playlist search toggle for skipping playlists who's gameobject is disabled.

### Changed
- Update Controls script to utilize the new usage of `videoDuration` and to properly display when the time is less than start time (for example if the AVPro buffer bug prevents the complete auto-seek that is expected, it will have the current time be a negative value)
- Change default automatic resync interval to 5 minutes.
- Update VideoManagerV2 to rework the configuration options to have clearer names as well as more precise purpose.  

### Fixed
- Fixed support for start/end time usage. 
    - Adds script variable `videoLength` to represent the full length of the backing video, where `videoDuration` now represents the amount of time between the start and end time of a video.
- Fixed initial volume not being assigned properly during the Start phase.


## 2.0 Beta 8.0 (2021-07-26)
### Added
- Added pagination to the playlist inspector for easier navigation.- Added playlist search prefab (part of the Playlist plugin system)
    - PROTIP: To add extra text to search by in a title, you can set the text size to any part of the title to 0  
    Such as: `Epic Meme Compilation #420 <size=0>2008 2012 ancient throwback classic </size>`
- Final reorganization of folder structure.
    - The root folder has been renamed from `TV` to `ProTV`
    - Updated documentation to reflect the update folder structure.
    - Anything that used to be in the `TV/Scripts/Plugins` folder is in their respective `ProTV/Plugins/*` folders.
    - All plugin specific files have been moved to the plugin specific folders (eg: `TV/Stuff/UI` -> `ProTv/Plugins/MediaControls/UI`)
    - The base `Stuff` folder has been removed in favor of individual folders.
- Add configuration options to `VideoManagerV2` for defining how the audio is handled during video player swap.
- Add missing and cleanup existing documentation.

### Changed
- Playlist titles are now no longer limited to 140 characters

### Removed
- Remove the ProTV v1 TVManager and VideoManager (the legacy ones that should no longer be in use anyways)

### Fixed
- Fixed playlist performance issues. 
- Fixed the MediaControls dropdown nested canvas issue (the one where the cursor hid parts of the menu)
- Fix improper queue behavior when the TV is in a locked state.

#### Known Issues
- If the owner has a video paused and a late joiner joins, the video won't be paused for them, it'll still play.
- (AVPro issue) Unable to seek to any point in the video until the download buffer (internal to AVPro) has reached that point.
- When testing locally, it is recommended NOT to disable the `Allow Master Control`. Due to an issue with how instance owner works locally, you will get locked out of the TV if you have `Locked By Default` enabled. This issue is NOT present once uploaded to VRChat servers, and can be safely disabled prior to uploading if the feature is needed.
- (*WHEN UPGRADING FROM BETA 6.8 OR PRIOR*) To complete the upgrade, you need to manually rename the file `SimplePlaylist.cs` to  `Playlist.cs`, which was located at `Assets/ArchiTechAnon/TV/Scripts/Plugins`, because unity hates file name changes apparently.
- (*WHEN UPGRADING FROM BETA 7.1 OR EARLIER*) If you have any playlists in your scene you will need to click the "Update Scene" button on each of them to regenerate the scene structure for the new click detection required for uncapped playlist entry count.


## 2.0 Beta 7.1 (2021-07-18)
### Added
- Added a configurable auto resync interval that will trigger a resync for both Audio/Video and time sync between users. 
    - This helps ensure tight and accurate playback between all users, even in certain low performance situations.
- Create folder `Assets/ArchiTechAnon/ProTV/Plugins` as the location for all plugin specific things to be moved to prior to official release.

### Changed
- Updated 360 video from a sphere mesh to a new custom skybox swap mechanism.
    - This is available as a prefab in `Assets/ArchiTechAnon/ProTV/Plugins/SkyboxSwapper`

### Removed
- Removed the `Playing Threshold` configuration option as it's no longer used.

### Fixed
- Fix improper implementation of _ChangeSeek* methods.
    - `_ChangeSeekTime` and `_ChangeSeekTimeTo(float)` now operate with an explicit time in seconds.  
    It uses the variable `IN_ChangeSeekTo_float_Seconds`.
    - Added `_ChangeSeekPercent` and `_ChangeSeekPercentTo(float)` to operate with a normalized percent value between 0.0 and 1.0.  
    It uses the variable `IN_ChangeSeekPercent_float_Percent`.
    It automatically takes into consideration any custom start and end time given via query parameters. 


## 2.0 Beta 7.0 (2021-07-12)
### Added
- Add new Queue plugin.
- Image support and Auto-Grid support added to the playlist plugin
- Example of a 360 video usage added to the demo scene.
- Added mitigations for certain audio/video desync issues.

### Changed
- Aspect-Ratio now renders correctly (Thanks Merlin ; Texelsaur!)
- Mitigated race condition for owner vs other when loading media.
- All TV events have been renamed from using the `_On` prefix to using the `_Tv` prefix to avoid naming confusion with normal udon events.
    - Example: `_OnPlay` would be `_TvPlay` and `_OnMediaStart` is now `_TvMediaStart`
    - NOTE: The outgoing variable names have also been updated respectively. Example: `OUT_OnOwnerChange_int_Id` is now `OUT_TvOwnerChange_int_Id`
- Simplified extension script and plugin names
    - `SimplePlaylist` is now just `Playlist`
    - Previously mentioned `LiveQueue` (new in this release) is going to be called simply `Queue`
- Renamed `allowMasterLockToggle` flag on `TVManagerV2` to `allowMasterControl` for clarity on how the flag is actually used.

### Fixed
- Fix various stability issues with live streams
- Fix some edge-case issues with autoplay.
- Fix the implementation of the MediaChange event to occur at the correct times.

#### Known issues
- If TV owner has the player paused, late joiners will still play the video on join until a sync check occurs (play/pause/stop/seek/etc).


## 2.0 Beta 6.8 (2021-06-19)
### Added
- Add livestream auto-reload for attempting to reload the stream after it goes down
- Add missing _OnMediaChange event trigger for non-owners
- Add instance owner (different than master) as always having access to control the TV (lock, change video, etc)

### Changed
- Minor logic optimizations
- Improve loading of autoplay video for non-owners

### Fixed
- Fix url sync from not being applied correctly for the local player
- Fix url not reloading when the owner puts in the same url as what the local user already has cached
- Fix livestream detection when the stream returns a length of 0 instead of Infinity


## 2.0 Beta 6 (2021-05-18)
### Added
- Add network lag compensation logic to improve sync time accuracy
- Add Resync UI action for triggering the sync enforcement for one frame (in case the video sync drifts)
- Add Reload UI action for explicitly doing a media reload with a single click (just does _Stop then _Play behind the scenes)

### Changed
- Update sync data to take advantage of the Udon Network Update (UNU) changes
- Move all occasional data into manually synced variables
- `BasicUI.cs` and `SlimUI.cs` have been merged into a single plugin `Controls_ActiveState.cs`
- Many refinements to the controls UI plugin
- Remove loop buttons; looping is now controlled exclusively by the loop url parameter
- Adjusted some UI layout parameters for better structure
- `BasicUI` plugin has been rebuilt as the `GeneralControls` plugin
- `SlimUI` plugin has been rebuilt as the `SlimControls` plugin
- `SlimUIReduced` plugin has been rebuilt as the `MinifiedControls` plugin
- Updated the example scene to account for the controls plugins changes
- Update playlist inspector to accept pulling playlist info from a custom txt file

### Removed
- Remove the playerId value from the info display text


## 2.0 Beta 5 (2021-04-30)
### Added
- Start using a formal CHANGELOG

### Changed
- Modify how time sync works. It now only enforces sync time from owner for the first few seconds, and then any time a state change of the TV affects the current sync time. Basically, the enforcement is a bit more lax to help support Quest playback better.
- Update the UIs to make use of the modified sync time activity. Sync button is now an actual "Resync" action, that will do a one-time activation of the sync time enforcement which will jump the video to the current time sync from the owner.


## 2.0 Beta 4 (2021-04-14)
### Changed
- Updated BasicUI prefab to be dark mode (permanently)
- Some structural cleanup in the example scene and prefabs.


## 2.0 Beta 3 (2021-04-12)
### Added
- Added new prefab modules
- Added new ready-made TV prefabs
- Added additional documentation with pictures
- Added new custom TV model commissioned from Chim-Cham

### Changed
- Updated the example scene
- Modified the layout of the folder structure for better asset organization
- Updated SimplePlaylist script to support progress bars (in the form of UI Sliders; Sleek Playlist Module makes use of this)

### Fixed
- Fix some edgecase video looping issues and a couple other bugs I can't remember


## 2.0 Beta 2 (2021-04-03)
### Added
- Added newline support to the display titles (can be combined with the richtext support to create video descriptions)

### Changed
- SimplePlaylist UI template adjustments (strictly visual)

### Fixed
- Fixed Video Playlist Items scroll area not sizing correctly


## 2.0 Beta 1 (2021-03-21)
### Added
- Added pub/sub style events system for extensibility
- Added playlist extension module
- Added a lot of spit, shine and elbow grease to make it more robust and self-correcting

### Changed
- Decoupled UI from the core TV functionality
- Modified the legacy UI into an extension module
